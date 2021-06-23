using NUnit.Framework;

namespace Tree
{
    [TestFixture]
    public class Test4Tree
    {
        [Test]
        public void TestTreeSum1()
        {
            //Unable to comprehend "including the value of the parent value" so that assuming only need to "sum the values of the nodes throughout the tree"
            TreeNode root = new TreeNode(null, 1);

            TreeNode secondFirst = new TreeNode(root, 2);
            TreeNode secondSecond = new TreeNode(root, 3);

            TreeNode thirdFirst = new TreeNode(secondFirst, 4);
            TreeNode thirdSecond = new TreeNode(secondFirst, 5);
            TreeNode thirdThird = new TreeNode(secondSecond, 6);

            TreeNode fouthFirst = new TreeNode(thirdFirst, 7);
            TreeNode fouthSecond = new TreeNode(thirdFirst, 8);
            TreeNode fouthThird = new TreeNode(thirdFirst, 9);

            Assert.IsTrue(root.Sum() == 1 + 2 + 3 + 4 + 5 + 6 + 7 + 8 + 9);
        }
    }
}
