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

    private readonly Uri wfsUri;

    public GeoMetaDataCollector(Uri wfsUri, ILogger logger = null) {
      this.wfsUri = wfsUri;
      this.logger = logger;
    }

    public IEnumerable<Layer> Layers() {
      var xmlResponse = this.GetXmlResponse($"{this.wfsUri.AbsoluteUri}?service=wfs&request=GetCapabilities");
      var nsmgr = GetNamespaceManager(xmlResponse);
      var layers = xmlResponse.SelectNodes("/wfs:WFS_Capabilities/wfs:FeatureTypeList/wfs:FeatureType", nsmgr).Cast<XmlElement>();
      return from layer in layers
             select new Layer {
               Name = layer.SelectSingleNode("wfs:Name", nsmgr)?.InnerText,
               Title = layer.SelectSingleNode("wfs:Title", nsmgr)?.InnerText,
               Abstract = layer.SelectSingleNode("wfs:Abstract", nsmgr)?.InnerText
             };
    }

    public IEnumerable<LayerTextField> LayerTextFields(string layer) {
      var xmlResponse = this.GetXmlResponse($"{this.wfsUri.AbsoluteUri}?service=wfs&request=DescribeFeatureType&typeName={layer}");
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
      var url = $"{this.wfsUri.AbsoluteUri}?service=wfs&request=GetFeature&typeName={layer}&outputformat=csv&PropertyName={field}";
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

    internal static string GetValueFromLine(string line) {
      var matches = Regex.Matches(line, "(?:,\"?)([^\"]*)(?:\"?)$");
      return (matches.Count == 1 && matches[0].Groups.Count == 2) ? matches[0].Groups[1].Value : string.Empty;
    }

    private static XmlNamespaceManager GetNamespaceManager(XmlDocument xmlCapabilities) {
      var namespaceManager = new XmlNamespaceManager(xmlCapabilities.NameTable);
      namespaceManager.AddNamespace("wfs", "http://www.opengis.net/wfs/2.0");
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
