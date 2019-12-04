namespace GeoMetaDataCollector {
  using System.Collections.Generic;

  public class Layer {
    public string Abstract { get; set; }

    public string Name { get; set; }

    public string Title { get; set; }

    public string Version { get; set; }

    public IEnumerable<string> Crs { get; set; }

    public IEnumerable<string> Format { get; set; }
  }
}
