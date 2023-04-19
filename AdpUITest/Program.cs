using AdpUISourceGeneration.Annotation;

namespace AdpUITest
{
    public partial class TestClass
    {
        [TMPText(AccessModifier.Private)] private string m_Text;
        [TMPText(AccessModifier.Public)] private string m_Text2;

        private void Test()
        {
            Text = "";
            Text2 = "";
        }
    }   
 
}


