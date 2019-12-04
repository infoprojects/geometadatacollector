namespace GeoMetaDataCollector {
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;
  using System.Net;
  using System.Text.RegularExpressions;
  using System.Xml;

  public class GeoMetaDataCollector {
    private readonly ILogger logger;

    private readonly Uri serviceUri;

    public GeoMetaDataCollector(Uri serviceUri, ILogger logger = null) {
      this.serviceUri = serviceUri;
      this.logger = logger;
    }

    public IEnumerable<Layer> Layers() {
      var xmlResponse = this.GetXmlResponse($"{this.serviceUri.AbsoluteUri}?service=wfs&request=GetCapabilities");
      var nsmgr = GetNamespaceManager(xmlResponse);
      var layers = xmlResponse.SelectNodes("/wfs:WFS_Capabilities/wfs:FeatureTypeList/wfs:FeatureType", nsmgr).Cast<XmlElement>();
      return from layer in layers
             select new Layer {
               Name = layer.SelectSingleNode("wfs:Name", nsmgr)?.InnerText,
               Title = layer.SelectSingleNode("wfs:Title", nsmgr)?.InnerText,
               Abstract = layer.SelectSingleNode("wfs:Abstract", nsmgr)?.InnerText
             };
    }

    public IEnumerable<Layer> MapLayers() {
      var xmlResponse = this.GetXmlResponse($"{this.serviceUri.AbsoluteUri}?service=WMS&request=GetCapabilities");
      var nsmgr = GetNamespaceManager(xmlResponse);
      var version = xmlResponse.SelectSingleNode("/wms:WMS_Capabilities/@version", nsmgr).Value;
      var formats = xmlResponse.SelectNodes("/wms:WMS_Capabilities/wms:Capability/wms:Request/wms:GetFeatureInfo/wms:Format", nsmgr)
        .Cast<XmlElement>()
        .Select(format => format.InnerText)
        .ToList();
      var layers = xmlResponse.SelectNodes("/wms:WMS_Capabilities/wms:Capability//wms:Layer[wms:Name]", nsmgr).Cast<XmlElement>();
      return from layer in layers
             select new Layer {
               Name = layer.SelectSingleNode("wms:Name", nsmgr)?.InnerText,
               Title = layer.SelectSingleNode("wms:Title", nsmgr)?.InnerText,
               Abstract = layer.SelectSingleNode("wms:Abstract", nsmgr)?.InnerText,
               Version = version,
               Format = formats,
               Crs = layer.SelectNodes("wms:CRS", nsmgr)
                 .Cast<XmlElement>()
                 .Select(crs => crs.InnerText)
                 .ToList()
             };
    }

    public IEnumerable<LayerTextField> LayerTextFields(string layer) {
      var xmlResponse = this.GetXmlResponse($"{this.serviceUri.AbsoluteUri}?service=wfs&request=DescribeFeatureType&typeName={layer}");
      var nsmgr = GetNamespaceManager(xmlResponse);
      return from featureType in xmlResponse.SelectNodes($"/xsd:schema/xsd:element[@type = '{layer}Type']", nsmgr).Cast<XmlElement>()
             let featureTypeName = featureType.GetAttribute("name")
             from textField in xmlResponse.SelectNodes($"/xsd:schema/xsd:complexType[@name = '{featureTypeName}Type']/xsd:complexContent/xsd:extension[@base = 'gml:AbstractFeatureType']/xsd:sequence/xsd:element[@type = 'xsd:string']", nsmgr).Cast<XmlElement>()
             select new LayerTextField {
               Name = textField.GetAttribute("name")
             };
    }

    public IEnumerable<LayerTextFieldValue> DistinctCollectionValues(string layer, string field) {
      var values = new List<string>();
      var url = $"{this.serviceUri.AbsoluteUri}?service=wfs&request=GetFeature&typeName={layer}&outputformat=csv&PropertyName={field}";
      this.logger?.Debug("Loading values from {0}", url);
      var request = WebRequest.Create(url);
      using (var response = request.GetResponse())
      using (var stream = response.GetResponseStream())
      using (var reader = new StreamReader(stream)) {
        var line = reader.ReadLine();
        this.logger?.Debug("Header: {0}", line);
        while (!reader.EndOfStream) {
          line = reader.ReadLine();
          this.logger?.Trace("Record: {0}", line);
          var value = GetValueFromLine(line);
          if (!string.IsNullOrEmpty(value) && !values.Contains(value)) {
            values.Add(value);
          }
        }
      }

      return values.Select(value => new LayerTextFieldValue { Value = value });
    }

    public IEnumerable<LayerTextFieldValue> DistinctSplittedCollectionValues(string layer, string field, string splitPattern = ",") {
      return (from unsplittedValue in this.DistinctCollectionValues(layer, field)
              from value in Regex.Split(unsplittedValue.Value, splitPattern)
              select value.Trim())
        .Distinct()
        .OrderBy(value => value)
        .Select(value => new LayerTextFieldValue { Value = value });
    }

    internal static string GetValueFromLine(string line) {
      var matches = Regex.Matches(line, "(?:,\"?)([^\"]*)(?:\"?)$");
      return (matches.Count == 1 && matches[0].Groups.Count == 2) ? matches[0].Groups[1].Value : string.Empty;
    }

    private static XmlNamespaceManager GetNamespaceManager(XmlDocument xmlCapabilities) {
      var namespaceManager = new XmlNamespaceManager(xmlCapabilities.NameTable);
      namespaceManager.AddNamespace("wfs", "http://www.opengis.net/wfs/2.0");
      namespaceManager.AddNamespace("wms", "http://www.opengis.net/wms");
      namespaceManager.AddNamespace("xsd", "http://www.w3.org/2001/XMLSchema");
      return namespaceManager;
    }

    private XmlDocument GetXmlResponse(string url) {
      var xmlDocument = new XmlDocument();
      this.logger?.Debug("Loading xml from {0}", url);
      xmlDocument.Load(url);
      this.logger?.Trace("Response {0}", xmlDocument.OuterXml);
      return xmlDocument;
    }
  }
}
