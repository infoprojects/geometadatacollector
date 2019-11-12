namespace GeoMetaDataCollector {
  using System;
  using System.Collections.Generic;
  using System.Linq;
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
      var xmlResponse = this.GetXmlResponse($"{this.wfsUri.AbsoluteUri}?request=DescribeFeatureType&typeName={layer}");
      var nsmgr = GetNamespaceManager(xmlResponse);
      return from featureType in xmlResponse.SelectNodes($"/xsd:schema/xsd:element[@type = '{layer}Type']", nsmgr).Cast<XmlElement>()
             let featureTypeName = featureType.GetAttribute("name")
             from textField in xmlResponse.SelectNodes($"/xsd:schema/xsd:complexType[@name = '{featureTypeName}Type']/xsd:complexContent/xsd:extension[@base = 'gml:AbstractFeatureType']/xsd:sequence/xsd:element[@type = 'xsd:string']", nsmgr).Cast<XmlElement>()
             select new LayerTextField {
               Name = textField.GetAttribute("name")
             };
    }

    public IEnumerable<LayerTextFieldValue> DistinctCollectionValues(string layer, string field) {
      throw new NotSupportedException();
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
