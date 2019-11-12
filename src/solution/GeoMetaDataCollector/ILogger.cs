namespace GeoMetaDataCollector {
  public interface ILogger {
    void Trace(string msg, params object[] values);

    void Debug(string msg, params object[] values);
  }
}
