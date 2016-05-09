namespace Microsoft.VisualStudio.Services.Agent.Tests.Worker.CodeCoverage
{
    public class CodeCoverageConstants
    {
        public static readonly string ValidJacocoXml =
             @"<?xml version='1.0' encoding='UTF-8' standalone='yes'?>
            <!DOCTYPE report PUBLIC '-//JACOCO//DTD Report 1.0//EN' 'report.dtd'>
            <report name = 'my-app' >
              <sessioninfo id='sgupta-dev-62e7c9fb' start='1437729720597' dump='1437729720957'/>
              <package name = 'com/mycompany/app' >
                <class name='com/mycompany/app/App2'>
                  <method name = '&lt;init&gt;' desc='()V' line='7'>
                    <counter type = 'INSTRUCTION' missed='3' covered='0'/>
                    <counter type = 'LINE' missed='1' covered='0'/>
                    <counter type = 'COMPLEXITY' missed='1' covered='0'/>
                    <counter type = 'METHOD' missed='1' covered='0'/>
                  </method>
                  <method name = 'sub' desc='(II)I' line='12'>
                    <counter type = 'INSTRUCTION' missed='0' covered='4'/>
                    <counter type = 'LINE' missed='0' covered='1'/>
                    <counter type = 'COMPLEXITY' missed='0' covered='1'/>
                    <counter type = 'METHOD' missed='0' covered='1'/>
                  </method>
                  <method name = 'mul' desc='(II)I' line='16'>
                    <counter type = 'INSTRUCTION' missed='4' covered='0'/>
                    <counter type = 'LINE' missed='1' covered='0'/>
                    <counter type = 'COMPLEXITY' missed='1' covered='0'/>
                    <counter type = 'METHOD' missed='1' covered='0'/>
                  </method>
                  <counter type = 'INSTRUCTION' missed='7' covered='4'/>
                  <counter type = 'LINE' missed='2' covered='1'/>
                  <counter type = 'COMPLEXITY' missed='2' covered='1'/>
                  <counter type = 'METHOD' missed='2' covered='1'/>
                  <counter type = 'CLASS' missed='0' covered='1'/>
                </class>
                <class name='com/mycompany/app/App'>
                  <method name = '&lt;init&gt;' desc='()V' line='7'>
                    <counter type = 'INSTRUCTION' missed='3' covered='0'/>
                    <counter type = 'LINE' missed='1' covered='0'/>
                    <counter type = 'COMPLEXITY' missed='1' covered='0'/>
                    <counter type = 'METHOD' missed='1' covered='0'/>
                  </method>
                  <method name = 'main' desc='([Ljava/lang/String;)V' line='11'>
                    <counter type = 'INSTRUCTION' missed='4' covered='0'/>
                    <counter type = 'LINE' missed='2' covered='0'/>
                    <counter type = 'COMPLEXITY' missed='1' covered='0'/>
                    <counter type = 'METHOD' missed='1' covered='0'/>
                  </method>
                  <method name = 'add' desc='(II)I' line='16'>
                    <counter type = 'INSTRUCTION' missed='0' covered='4'/>
                    <counter type = 'LINE' missed='0' covered='1'/>
                    <counter type = 'COMPLEXITY' missed='0' covered='1'/>
                    <counter type = 'METHOD' missed='0' covered='1'/>
                  </method>
                  <counter type = 'INSTRUCTION' missed='7' covered='4'/>
                  <counter type = 'LINE' missed='3' covered='1'/>
                  <counter type = 'COMPLEXITY' missed='2' covered='1'/>
                  <counter type = 'METHOD' missed='2' covered='1'/>
                  <counter type = 'CLASS' missed='0' covered='1'/>
                </class>
                <sourcefile name = 'App.java' >
                  <line nr='7' mi='3' ci='0' mb='0' cb='0'/>
                  <line nr = '11' mi='3' ci='0' mb='0' cb='0'/>
                  <line nr = '12' mi='1' ci='0' mb='0' cb='0'/>
                  <line nr = '16' mi='0' ci='4' mb='0' cb='0'/>
                  <counter type = 'INSTRUCTION' missed='7' covered='4'/>
                  <counter type = 'LINE' missed='3' covered='1'/>
                  <counter type = 'COMPLEXITY' missed='2' covered='1'/>
                  <counter type = 'METHOD' missed='2' covered='1'/>
                  <counter type = 'CLASS' missed='0' covered='1'/>
                </sourcefile>
                <sourcefile name = 'App2.java' >
                  <line nr='7' mi='3' ci='0' mb='0' cb='0'/>
                  <line nr = '12' mi='0' ci='4' mb='0' cb='0'/>
                  <line nr = '16' mi='4' ci='0' mb='0' cb='0'/>
                  <counter type = 'INSTRUCTION' missed='7' covered='4'/>
                  <counter type = 'LINE' missed='2' covered='1'/>
                  <counter type = 'COMPLEXITY' missed='2' covered='1'/>
                  <counter type = 'METHOD' missed='2' covered='1'/>
                  <counter type = 'CLASS' missed='0' covered='1'/>
                </sourcefile>
                <counter type = 'INSTRUCTION' missed='14' covered='8'/>
                <counter type = 'LINE' missed='5' covered='2'/>
                <counter type = 'COMPLEXITY' missed='4' covered='2'/>
                <counter type = 'METHOD' missed='4' covered='2'/>
                <counter type = 'CLASS' missed='0' covered='2'/>
              </package>
              <counter type = 'INSTRUCTION' missed='14' covered='8'/>
              <counter type = 'LINE' missed='5' covered='2'/>
              <counter type = 'COMPLEXITY' missed='4' covered='2'/>
              <counter type = 'METHOD' missed='4' covered='2'/>
              <counter type = 'CLASS' missed='0' covered='2'/>
            </report>";
        public static readonly string ValidCoberturaXml =
            @"<?xml version='1.0'?>
            <!DOCTYPE coverage SYSTEM 'http://cobertura.sourceforge.net/xml/coverage-04.dtd'>

            <coverage line-rate='0.5' branch-rate='1.0' lines-covered='11' lines-valid='22' branches-covered='2.4' branches-valid='8.8' complexity='1.0' version='2.1.1' timestamp='1444371063012'>
	            <sources>
		            <source>F:/Test/ant/TestCode/Ant/src</source>
	            </sources>
	            <packages>
		            <package name = '' line-rate='0.5' branch-rate='1.0' complexity='1.0'>
			            <classes>
				            <class name='SampleUtil' filename='SampleUtil.java' line-rate='0.6666666666666666' branch-rate='1.0' complexity='1.0'>
					            <methods>
						            <method name = '&lt;init&gt;' signature='(Ljava/lang/String;)V' line-rate='1.0' branch-rate='1.0' complexity='0'>
							            <lines>
								            <line number = '6' hits='2' branch='false'/>
								            <line number = '7' hits='2' branch='false'/>
								            <line number = '8' hits='2' branch='false'/>
							            </lines>
						            </method>
						            <method name = 'GetMessage' signature='()Ljava/lang/String;' line-rate='1.0' branch-rate='1.0' complexity='0'>
							            <lines>
								            <line number = '18' hits='1' branch='false'/>
							            </lines>
						            </method>
						            <method name = 'printMessage' signature='()Ljava/lang/String;' line-rate='0.0' branch-rate='1.0' complexity='0'>
							            <lines>
								            <line number = '12' hits='0' branch='false'/>
								            <line number = '13' hits='0' branch='false'/>
							            </lines>
						            </method>
					            </methods>
					            <lines>
						            <line number = '6' hits='2' branch='false'/>
						            <line number = '7' hits='2' branch='false'/>
						            <line number = '8' hits='2' branch='false'/>
						            <line number = '12' hits='0' branch='false'/>
						            <line number = '13' hits='0' branch='false'/>
						            <line number = '18' hits='1' branch='false'/>
					            </lines>
				            </class>
				            <class name='UtilTest' filename='UtilTest.java' line-rate='0.875' branch-rate='1.0' complexity='1.0'>
					            <methods>
						            <method name = '&lt;init&gt;' signature='()V' line-rate='1.0' branch-rate='1.0' complexity='0'>
							            <lines>
								            <line number = '5' hits='2' branch='false'/>
								            <line number = '6' hits='2' branch='false'/>
								            <line number = '7' hits='2' branch='false'/>
							            </lines>
						            </method>
						            <method name = 'testMethod1' signature='()V' line-rate='1.0' branch-rate='1.0' complexity='0'>
							            <lines>
								            <line number = '11' hits='1' branch='false'/>
								            <line number = '12' hits='1' branch='false'/>
							            </lines>
						            </method>
						            <method name = 'testMethod2' signature='()V' line-rate='0.6666666666666666' branch-rate='1.0' complexity='0'>
							            <lines>
								            <line number = '16' hits='1' branch='false'/>
								            <line number = '17' hits='1' branch='false'/>
								            <line number = '18' hits='0' branch='false'/>
							            </lines>
						            </method>
					            </methods>
					            <lines>
						            <line number = '5' hits='2' branch='false'/>
						            <line number = '6' hits='2' branch='false'/>
						            <line number = '7' hits='2' branch='false'/>
						            <line number = '11' hits='1' branch='false'/>
						            <line number = '12' hits='1' branch='false'/>
						            <line number = '16' hits='1' branch='false'/>
						            <line number = '17' hits='1' branch='false'/>
						            <line number = '18' hits='0' branch='false'/>
					            </lines>
				            </class>
				            <class name='UtilTest2' filename='UtilTest2.java' line-rate='0.0' branch-rate='1.0' complexity='1.0'>
					            <methods>
						            <method name = '&lt;init&gt;' signature='()V' line-rate='0.0' branch-rate='1.0' complexity='0'>
							            <lines>
								            <line number = '5' hits='0' branch='false'/>
								            <line number = '6' hits='0' branch='false'/>
								            <line number = '7' hits='0' branch='false'/>
							            </lines>
						            </method>
						            <method name = 'testMethod1' signature='()V' line-rate='0.0' branch-rate='1.0' complexity='0'>
							            <lines>
								            <line number = '11' hits='0' branch='false'/>
								            <line number = '12' hits='0' branch='false'/>
							            </lines>
						            </method>
						            <method name = 'testMethod2' signature='()V' line-rate='0.0' branch-rate='1.0' complexity='0'>
							            <lines>
								            <line number = '16' hits='0' branch='false'/>
								            <line number = '17' hits='0' branch='false'/>
								            <line number = '18' hits='0' branch='false'/>
							            </lines>
						            </method>
					            </methods>
					            <lines>
						            <line number = '5' hits='0' branch='false'/>
						            <line number = '6' hits='0' branch='false'/>
						            <line number = '7' hits='0' branch='false'/>
						            <line number = '11' hits='0' branch='false'/>
						            <line number = '12' hits='0' branch='false'/>
						            <line number = '16' hits='0' branch='false'/>
						            <line number = '17' hits='0' branch='false'/>
						            <line number = '18' hits='0' branch='false'/>
					            </lines>
				            </class>
			            </classes>
		            </package>
	            </packages>
            </coverage>";
    }
}
