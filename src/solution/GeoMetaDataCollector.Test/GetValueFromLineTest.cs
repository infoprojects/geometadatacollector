namespace GeoMetaDataCollector.Test {
  using System.Collections;

  using NUnit.Framework;

  public class GetValueFromLineTest {
    private static IEnumerable TestData {
      get {
        yield return new TestCaseData(
          "mepduinenobs.2386,Elytri juncea s. boreo",
          "Elytri juncea s. boreo");
        yield return new TestCaseData(
          "\"mepduinenobs.2341\",\"Sonchus arvensis v. maritimus\"",
          "Sonchus arvensis v. maritimus");
        yield return new TestCaseData(
          "mepduinenobs.2391,\",-\"",
          ",-");
        yield return new TestCaseData(
          "rwsbenthos_zwdelta_tijdelijk.fid-3dc4ec93_16e63fb41e9_189d,\"Gewoon porceleinkrabbetje, porseleinkrabbetje\"",
          "Gewoon porceleinkrabbetje, porseleinkrabbetje");
      }
    }

    [Test]
    [TestCaseSource(nameof(TestData))]
    public void GetValueFromLine(string line, string value) {
      var result = GeoMetaDataCollector.GetValueFromLine(line);
      Assert.AreEqual(value, result);
    }
  }
}
