namespace GeoMetaDataCollector {
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Xml;

  public class GeoMetaDataCollector {
    private readonly Uri wfsUri;

    private readonly ILogger logger;

    public GeoMetaDataCollector(Uri wfsUri, ILogger logger) {
      this.wfsUri = wfsUri;
      this.logger = logger;
    }

    public IEnumerable<Layer> Layers() {
      var xmlCapabilities = new XmlDocument();
      var urlCapabilities = $"{this.wfsUri.AbsoluteUri}?service=wfs&request=GetCapabilities";
      this.logger?.Debug("Loading xml from {0}", urlCapabilities);
      xmlCapabilities.Load(urlCapabilities);
      this.logger?.Trace("Response {0}", xmlCapabilities.OuterXml);
      var namespaceManager = new XmlNamespaceManager(xmlCapabilities.NameTable);
      namespaceManager.AddNamespace("wfs", "http://www.opengis.net/wfs/2.0");
      var featureTypes = xmlCapabilities.SelectNodes("/wfs:WFS_Capabilities/wfs:FeatureTypeList/wfs:FeatureType", namespaceManager).Cast<XmlElement>();
      return from featureType in featureTypes
             select new Layer {
               Name = featureType.SelectSingleNode("wfs:Name", namespaceManager)?.InnerText,
               Title = featureType.SelectSingleNode("wfs:Title", namespaceManager)?.InnerText,
               Abstract = featureType.SelectSingleNode("wfs:Abstract", namespaceManager)?.InnerText
             };
    }

    public IEnumerable<LayerTextField> LayerTextFields(string layer) {
      throw new NotSupportedException();
    }

    public IEnumerable<LayerTextFieldValue> DistinctCollectionValues(string layer, string field) {
      throw new NotSupportedException();
    }
  }
}
