namespace Runner.Server.Azure.Devops
{
    public class AzurePipelinesUtilsTests {
        [Fact]
        public void TestYAMLToJson() {
            string json = AzurePipelinesUtils.YAMLToJson("test");
            Assert.Equal("\"test\"", json);
        }
    }
}