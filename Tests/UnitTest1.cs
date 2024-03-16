namespace Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            Assert.Pass();
        }

        [Test]
        public void TestNullableBoolCheck()
        {
            bool f1 = false;
            bool? f2 = null;

            if (f2 != f1)
                f2 = null;

            Assert.AreEqual(null, f2);
        }
    }
}