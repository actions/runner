namespace Microsoft.VisualStudio.Services.Agent.Tests.Worker.CodeCoverage
{
    public class CodeCoverageConstants
    {
        #region publish CC files
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
        #endregion

        #region cobertura ant files
        public static readonly string BuildXml =
            @"<?xml version='1.0'?>
            <project name ='sample'>
              <target name='tests'>
                <junit>
                  <test name = 'org.jacoco.examples.HelloJaCoCoTest' />
                  <classpath>
                    <pathelement location='./bin' />
                  </classpath>
                  <batchtest todir='${junit.out.dir.xml}'>
                    <fileset dir = '${classes.dir}' includes='*'/>
                  </batchtest>
                </junit>
              </target>
            </project>";

        public static readonly string BuildWithCCCoberturaXml =
            @"<?xml version='1.0'?>
            <project name = 'sample' >
              <path id='cobertura.classpath' description='classpath for instrumenting classes'>
                <pathelement location = 'cobertura-2.1.1/cobertura-2.1.1.jar' />
                <fileset dir='.'>
                  <include name = 'cobertura-2.1.1/lib/**/*.jar' />
                </fileset >
              </path >
              <taskdef classpathref='cobertura.classpath' resource='tasks.properties' />
              <target name = 'IntrumentClassses' >
                <delete file='cobertura.ser' />
                <cobertura-instrument todir = 'InstrumentedClasses' >
                  <fileset dir='class1'>
                    <include name = 'com.*.*' />
                    <exclude name='my.com.*.*' />
                  </fileset>
                  <fileset dir = 'class2' >
                    <include name='com.*.*' />        
                    <exclude name = 'a.b.*' />
                    <exclude name='my.com.*.*' />
                  </fileset>
                </cobertura-instrument>
              </target>
              <target name = 'tests' >
                <junit fork='true' forkmode='once'>
                  <sysproperty key = 'net.sourceforge.cobertura.datafile' file='cobertura.ser' />
                  <classpath location = 'InstrumentedClasses' />
                  <test name='org.jacoco.examples.HelloJaCoCoTest' />
                  <classpath>
                    <pathelement location = './bin' />
                  </classpath >
                  <batchtest todir='${junit.out.dir.xml}' fork='true'>
                    <fileset dir = '${classes.dir}' includes='*' />
                  </batchtest>
                  <classpath refid = 'cobertura.classpath' />
                </junit >
              </target >
              <target name='CoverageReport'>
                <cobertura-report format = 'html' destdir='codeCoverage' datafile='cobertura.ser' srcdir='src' />
              </target>
            </project>";

        public static readonly string BuildWithMultipleNodesXml =
            @"<?xml version='1.0'?>
            <project name ='sample'>
              <target name='tests'>
                <junit>
                  <test name = 'org.jacoco.examples.HelloJaCoCoTest' />
                  <classpath>
                    <pathelement location='./bin' />
                  </classpath>
                </junit>
                <junit>
                  <test name = 'org.jacoco.examples.HelloJaCoCoTest2' />
                  <batchtest fork='no' todir='${junit.out.dir.xml}'>
                    <fileset dir = '${classes.dir}' includes='*'/>
                  </batchtest>
                  <classpath>
                    <pathelement location = './bin' />
                  </classpath >
                </junit >
              </target >
            </project > ";

        public static readonly string InvalidBuildXml =
            @"<?xml version='1.0'?>
            <project name ='sample'>
              <target name='tests'>
                <junit>
                  <test name = 'org.jacoco.examples.HelloJaCoCoTest' />
                  <classpath>
                    <pathelement location='./bin' />
                  </classpath>
                </junit>
            </project>";

        public static readonly string BuildWithNoTestsXml =
            @"<?xml version='1.0'?>
            <project name ='sample'>
              <property name='src.dir' value='src'/>
              <property name = 'lib.dir' value='lib'/>
              <property name = 'target.dir' value='target'/>
              <property name = 'classes.dir' value='${target.dir}/classes'/>
              <target name = 'compile' >
                <javac debug='true' destdir='${classes.dir}'>
                  <src path = '${src.dir}' />
                  <classpath >
                    <fileset dir='${lib.dir}' includes='**/*.jar'/>
                  </classpath>
                </javac>
              </target>
            </project>";
        #endregion

        #region jacoco ant files
        public static readonly string BuildWithCCJacocoXml =
            @"<?xml version='1.0'?>
            <project name ='sample'>
              <target name='tests'>
                <jacoco:coverage destfile='target/jacoco.exec' includes='test.All*' excludes ='test.Exa*' xmlns:jacoco='antlib:org.jacoco.ant'>
                  <junit>
                    <test name = 'org.jacoco.examples.HelloJaCoCoTest' />
                    <classpath >
                      <pathelement location='./bin'/>
                    </classpath>
                    <batchtest fork='no' todir='${junit.out.dir.xml}'>
                      <fileset dir = '${classes.dir}' includes='*'/>
                    </batchtest>
                  </junit>
                </jacoco:coverage>
              </target>
              <target name = 'report' >
                <jacoco:report xmlns:jacoco='antlib:org.jacoco.ant' />
              </target>
            </project>";
        #endregion

        #region cobertura gradle files
        public static readonly string BuildGradle =
            @"apply plugin: 'java''
            apply plugin: 'maven'

            group = 'com.mycompany.app'
            version = '1.0-SNAPSHOT'
            description = '''my-app'''
            sourceCompatibility = 1.5
            targetCompatibility = 1.5

            repositories {
                maven {
                    url 'http://repo.maven.apache.org/maven2'
                }
            }

            dependencies {
                testCompile group: 'junit',
                name: 'junit',
                version: '3.8.1'
            }";

        public static readonly string BuildWithCCCoberturaGradle =
            @"plugins {
                id 'net.saliman.cobertura' version '2.2.7'
            }

            apply plugin: 'java'
            apply plugin: 'maven'

            group = 'com.mycompany.app'
            version = '1.0-SNAPSHOT'
            description = '''my-app'''
            sourceCompatibility = 1.5
            targetCompatibility = 1.5

            repositories {
                maven {
                    url 'http://repo.maven.apache.org/maven2'
                }
            }

            dependencies {
                testCompile group: 'junit',
                name: 'junit',
                version: '3.8.1'
            }

            allprojects {
	            repositories {
                    mavenCentral()
                }
            }

            dependencies {
	            testCompile 'org.slf4j:slf4j-api:1.7.12'
            }

            cobertura {
                coverageFormats = [ 'xml', 'html' ]
            }";

        public static readonly string BuildMultiModuleGradle =
            @"apply plugin: 'java''
            apply plugin: 'maven'

            group = 'com.mycompany.app'
            version = '1.0-SNAPSHOT'
            description = '''my-app'''
            sourceCompatibility = 1.5
            targetCompatibility = 1.5

            repositories {
                maven {
                    url 'http://repo.maven.apache.org/maven2'
                }
            }

            dependencies {
                testCompile group: 'junit',
                name: 'junit',
                version: '3.8.1'
            }";

        public static readonly string BuildWithCCMultiModuleGradle =
            @"apply plugin: 'java''
            apply plugin: 'maven'
            apply plugin: 'jacoco'

            group = 'com.mycompany.app'
            version = '1.0-SNAPSHOT'
            description = '''my-app'''
            sourceCompatibility = 1.5
            targetCompatibility = 1.5

            repositories {
                maven {
                    url 'http://repo.maven.apache.org/maven2'
                }
            }

            dependencies {
                testCompile group: 'junit',
                name: 'junit',
                version: '3.8.1'
            }

            jacocoTestReport {
                reports {
                    xml.enabled true
                    csv.enabled false
                    html.destination '${buildDir}/jacocoHtml'
                }
            }";
        #endregion

        #region jacoco gradle files
        public static readonly string BuildWithCCJacocoGradle =
            @"apply plugin: 'java''
            apply plugin: 'maven'
            apply plugin: 'jacoco'

            group = 'com.mycompany.app'
            version = '1.0-SNAPSHOT'
            description = '''my-app'''
            sourceCompatibility = 1.5
            targetCompatibility = 1.5

            repositories {
                maven {
                    url 'http://repo.maven.apache.org/maven2'
                }
            }

            dependencies {
                testCompile group: 'junit',
                name: 'junit',
                version: '3.8.1'
            }

            jacocoTestReport {
                reports {
                    xml.enabled true
                    csv.enabled false
                    html.destination '${buildDir}/jacocoHtml'
                }
            }";
        #endregion

        #region cobertura maven files
        public static readonly string PomXml =
            @"<?xml version='1.0' encoding='utf-8'?>
            <project xmlns = 'http://maven.apache.org/POM/4.0.0' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xsi:schemaLocation='http://maven.apache.org/POM/4.0.0 http://maven.apache.org/maven-v4_0_0.xsd'>
              <modelVersion>4.0.0</modelVersion>
              <groupId>com.mycompany.app</groupId>
              <artifactId>my-app</artifactId>
              <packaging>jar</packaging>
              <version>1.0-SNAPSHOT</version>
              <name>my-app</name>
              <url>http://maven.apache.org</url>
              <build>
                <plugins>
                </plugins>
                <pluginManagement>
                  <plugins>
                    <plugin>
          
                    </plugin>
                  </plugins>
                </pluginManagement>  
            </build>
              <dependencies>
                <dependency>
                  <groupId>junit</groupId>
                  <artifactId>junit</artifactId>
                  <version>3.8.1</version>
                  <scope>test</scope>
                </dependency>
              </dependencies>
            </project>";

        public static readonly string PomWithCCCoberturaXml =
            @"<project xmlns='http://maven.apache.org/POM/4.0.0' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xsi:schemaLocation='http://maven.apache.org/POM/4.0.0 http://maven.apache.org/maven-v4_0_0.xsd'>
              <modelVersion>4.0.0</modelVersion>
              <groupId>com.mycompany.app</groupId>
              <artifactId>my-app</artifactId>
              <packaging>jar</packaging>
              <version>1.0-SNAPSHOT</version>
              <name>my-app</name>
              <url>http://maven.apache.org</url>
              <build>
                <plugins>
                  <plugin>
                    <groupId>org.codehaus.mojo</groupId>
                    <artifactId>cobertura-maven-plugin</artifactId>
                    <version>2.7</version>
                    <configuration>
                      <formats>
                        <format>xml</format>
                        <format>html</format>
                      </formats>
                      <instrumentation>
                        <includes>
                          <include>com.*.*</include>
                          <include>app.me*.*</include>
                        </includes>
                        <excludes>
                          <exclude>me.*.*</exclude>
                          <exclude>a.b.*</exclude>
                          <exclude>my.com.*.*</exclude>
                        </excludes>
                      </instrumentation>
                      <RandomTag></RandomTag>
                    </configuration>
                    <executions>
                      <execution>
                        <id>package-3dbd177b-1c6b-4483-ba65-988711792c3d</id>
                        <goals>
                          <goal>cobertura</goal>
                        </goals>
                        <phase>package</phase>
                      </execution>
                    </executions>
                  </plugin>
                </plugins>
                <pluginManagement>
                  <plugins>
                    <plugin>
                      <groupId>org.codehaus.mojo</groupId>
                      <artifactId>cobertura-maven-plugin</artifactId>
                      <version>2.7</version>
                      <configuration>
                        <formats>
                          <format>xml</format>
                          <format>html</format>
                        </formats>
                        <instrumentation>
                          <includes>
                            <include>com.*.*</include>
                            <include>app.me*.*</include>
                          </includes>
                          <excludes>
                            <exclude>me.*.*</exclude>
                            <exclude>a.b.*</exclude>
                            <exclude>my.com.*.*</exclude>
                          </excludes>
                        </instrumentation>
                        <RandomTag></RandomTag>
                      </configuration>
                      <executions>
                        <execution>
                          <id>package-3dbd177b-1c6b-4483-ba65-988711792c3d</id>
                          <goals>
                            <goal>cobertura</goal>
                          </goals>
                          <phase>package</phase>
                        </execution>
                      </executions>
                    </plugin>
                  </plugins>
                </pluginManagement>
              </build>
              <dependencies>
                <dependency>
                  <groupId>junit</groupId>
                  <artifactId>junit</artifactId>
                  <version>3.8.1</version>
                  <scope>test</scope>
                </dependency>
              </dependencies>
              <reporting>
                <plugins>
                  <plugin>
                    <groupId>org.codehaus.mojo</groupId>
                    <artifactId>cobertura-maven-plugin</artifactId>
                    <version>2.7</version>
                    <configuration>
                      <formats>
                        <format>xml</format>
                        <format>html</format>
                      </formats>
                    </configuration>
                  </plugin>
                </plugins>
              </reporting>
            </project>";

        public static readonly string PomWithMultiModuleXml =
            @"<?xml version='1.0' encoding='utf-8'?>
            <project xmlns = 'http://maven.apache.org/POM/4.0.0' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xsi:schemaLocation='http://maven.apache.org/POM/4.0.0 http://maven.apache.org/maven-v4_0_0.xsd'>
              <modelVersion>4.0.0</modelVersion>
              <groupId>com.mycompany.app</groupId>
              <artifactId>my-app</artifactId>
              <packaging>pom</packaging>
              <version>1.0-SNAPSHOT</version>
              <name>my-app</name>
              <url>http://maven.apache.org</url>
              <modules>
                <module>module-1</module>
                <module>module-2</module>
              </modules>
              <build>
                <plugins>
                </plugins>
              </build>
              <dependencies>
                <dependency>
                  <groupId>junit</groupId>
                  <artifactId>junit</artifactId>
                  <version>3.8.1</version>
                  <scope>test</scope>
                </dependency>
              </dependencies>
            </project>";

        public static readonly string PomWithMultiModuleWithCCCoberturaXml =
            @"<?xml version='1.0' encoding='utf-8'?>
            <project xmlns = 'http://maven.apache.org/POM/4.0.0' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xsi:schemaLocation='http://maven.apache.org/POM/4.0.0 http://maven.apache.org/xsd/maven-4.0.0.xsd'>
              <modelVersion>4.0.0</modelVersion>
              <groupId>com.mlesniak.jacoco</groupId>
              <artifactId>module-main</artifactId>
              <version>1.0-SNAPSHOT</version>
              <packaging>pom</packaging>
              <modules>
                <module>module-1</module>
                <module>module-2</module>
              </modules>
              <properties>
                <project.build.sourceEncoding>UTF-8</project.build.sourceEncoding>
              </properties>
              <build>
                <plugins>
                  <plugin>
                    <groupId>org.apache.maven.plugins</groupId>
                    <artifactId>maven-surefire-plugin</artifactId>
                    <version>2.15</version>
                    <configuration>
                      <argLine>${surefireArgLine
                }</argLine>          
                    </configuration>
                  </plugin>
                  <plugin>
                    <groupId>org.codehaus.mojo</groupId>
                    <artifactId>cobertura-maven-plugin</artifactId>
                    <version>2.7</version>
                    <configuration>
                      <formats>
                        <format>xml</format>
                        <format>html</format>
                      </formats>
                      <instrumentation>
                        <includes />
                        <excludes />
                      </instrumentation>
                    </configuration>
                    <executions>
                      <execution>
                        <id>package</id>
                        <phase>package</phase>
                        <goals>
                          <goal>cobertura</goal>
                        </goals>
                      </execution>
                    </executions>
                  </plugin>      
                </plugins>
              </build>
              <reporting>
                <plugins>
                  <plugin>
                    <groupId>org.codehaus.mojo</groupId>
                    <artifactId>cobertura-maven-plugin</artifactId>
                    <version>2.7</version>
                    <configuration>
                      <formats>
                        <format>xml</format>
                        <format>html</format>
                      </formats>
                    </configuration>
                  </plugin>
                </plugins>
              </reporting>
              <dependencies>
                <dependency>
                  <groupId>junit</groupId>
                  <artifactId>junit</artifactId>
                  <version>4.11</version>
                  <scope>test</scope>
                </dependency>
              </dependencies>
            </project>
            ";

        public static readonly string CodeSearchPomXml =
            @"<?xml version='1.0' encoding='UTF-8'?>
            <project xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'
                     xmlns='http://maven.apache.org/POM/4.0.0'
                     xsi:schemaLocation='http://maven.apache.org/POM/4.0.0 http://maven.apache.org/xsd/maven-4.0.0.xsd'>
                <modelVersion>4.0.0</modelVersion>

                <groupId>com.microsoft.search</groupId>
                <artifactId>codesearch</artifactId>
                <version>6.0</version>

                <properties>
                    <!-- Versions info  -->
                    <googleCollectionVersion>1.0</googleCollectionVersion>
                    <elasticSearchVersion>1.7.1-SNAPSHOT</elasticSearchVersion>
                    <junitVersion>4.11</junitVersion>
                    <mavenCompilerVersion>3.3</mavenCompilerVersion>
                    <javaVersion>1.7</javaVersion>
                    <projectBuildVersion>${project.version
                }.${buildNumber
            }</projectBuildVersion>

                    <!--Artifactory uri-->
                    <artifactoryUri>http://almsearchbm:8081/artifactory</artifactoryUri>

                </properties>

                <repositories>
                    <repository>
                        <id>central</id>
                        <url>${artifactoryUri}/libs-release</url>
                        <snapshots>
                            <enabled>false</enabled>
                        </snapshots>
                    </repository>
                    <repository>
                        <id>snapshots</id>
                        <url>${artifactoryUri}/libs-snapshot</url>
                        <releases>
                            <enabled>false</enabled>
                        </releases>
                    </repository>
                </repositories>
                <pluginRepositories>
                    <pluginRepository>
                        <id>central</id>
                        <url>${artifactoryUri}/plugins-release</url>
                        <snapshots>
                            <enabled>false</enabled>
                        </snapshots>
                    </pluginRepository>
                    <pluginRepository>
                        <id>snapshots</id>
                        <url>${artifactoryUri}/plugins-snapshot</url>
                        <releases>
                            <enabled>false</enabled>
                        </releases>
                    </pluginRepository>
                </pluginRepositories>

                <scm>
                    <connection>scm:git:http://mseng.visualstudio.com//DefaultCollection/VSOnline/_git/VSO</connection>
                    <developerConnection>scm:git:http://mseng.visualstudio.com//DefaultCollection/VSOnline/_git/VSO</developerConnection>
                    <tag>HEAD</tag>
                    <url>http://mseng.visualstudio.com//DefaultCollection/VSOnline/_git/VSO</url>
                </scm>


                <dependencies>
                    <dependency>
                        <groupId>com.google.collections</groupId>
                        <artifactId>google-collections</artifactId>
                        <version>${googleCollectionVersion}</version>
                    </dependency>
                    <dependency>
                        <groupId>org.elasticsearch</groupId>
                        <artifactId>elasticsearch</artifactId>
                        <version>${elasticSearchVersion}</version>
                    </dependency>
                    <dependency>
                        <groupId>junit</groupId>
                        <artifactId>junit</artifactId>
                        <version>${junitVersion}</version>
                        <scope>test</scope>
                    </dependency>
                </dependencies>
                <build>
                    <resources>
                        <resource>
                            <directory>src/main/resources</directory>
                            <filtering>true</filtering>
                            <includes>
                                <include>*.properties</include>
                            </includes>
                        </resource>
                    </resources>
                    <plugins>
                        <plugin>
                            <groupId>org.apache.maven.plugins</groupId>
                            <artifactId>maven-compiler-plugin</artifactId>
                            <version>${mavenCompilerVersion}</version>
                            <configuration>
                                <source>${javaVersion}</source>
                                <target>${javaVersion}</target>
                            </configuration>
                        </plugin>
                        <plugin>
                            <groupId>org.codehaus.mojo1</groupId>
                            <artifactId>buildnumber-maven-plugin</artifactId>
                            <version>1.3</version>
                            <executions>
                                <execution>
                                    <id>buildnumber</id>
                                    <phase>validate</phase>
                                    <goals>
                                        <goal>create</goal>
                                    </goals>
                                </execution>
                            </executions>
                            <configuration>
                                <format>{0,number}</format>
                                <items>
                                    <item>buildNumber</item>
                                </items>
                                <doCheck>false</doCheck>
                                <doUpdate>false</doUpdate>
                                <revisionOnScmFailure>unknownbuild</revisionOnScmFailure>
                                <buildNumberPropertiesFileLocation>${buildNumberFilePathLocation}</buildNumberPropertiesFileLocation>
                            </configuration>
                        </plugin>
                        <plugin>
                            <groupId>org.apache.maven.plugins</groupId>
                            <artifactId>maven-jar-plugin</artifactId>
                            <version>2.1</version>
                            <configuration>
                                <archive>
                                    <manifest>
                                        <addDefaultImplementationEntries>true</addDefaultImplementationEntries>
                                    </manifest>
                                    <manifestEntries>
                                        <Implementation-Build>${buildNumber}</Implementation-Build>
                                        <Implementation-Vendor-Id>>${project.groupId}</Implementation-Vendor-Id>
                                    </manifestEntries>
                                </archive>
                            </configuration>
                        </plugin>
                    </plugins>

                    <!-- version will be<major>.<minor>.<buidnumber>-->
                    <finalName>${project.artifactId}-${projectBuildVersion}</finalName>
                </build>


            </project>";

        public static readonly string LogAppenderPomXml =
            @"<?xml version='1.0' encoding='UTF-8'?>
            <project xmlns = 'http://maven.apache.org/POM/4.0.0'
                     xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'
                     xsi:schemaLocation='http://maven.apache.org/POM/4.0.0 http://maven.apache.org/xsd/maven-4.0.0.xsd'>
                <modelVersion>4.0.0</modelVersion>

                <groupId>com.microsoft.log4jappender</groupId>
                <artifactId>ETWAppender</artifactId>
                <version>1.0-SNAPSHOT</version>
                <packaging>jar</packaging>

                <properties>
                    <jdk.version>1.7</jdk.version>
                    <log4j.version>1.2.17</log4j.version>
                    <mvnCompilerPlugin.version>2.3.2</mvnCompilerPlugin.version>
                </properties>

                <dependencies>
                    <dependency>
                        <groupId>org.slf4j</groupId>
                        <artifactId>nlog4j</artifactId>
                        <version>${log4j.version
                }</version>
                    </dependency>
                </dependencies>
                <build>
                    <plugins>
                        <plugin>
                            <groupId>org.apache.maven.plugins</groupId>
                            <artifactId>maven-compiler-plugin</artifactId>
                            <version>${mvnCompilerPlugin.version
            }</version>
                            <configuration>
                                <source>${jdk.version}</source>
                                <target>${jdk.version}</target>
                            </configuration>
                        </plugin>
                    </plugins>
                </build>
            </project>";
        #endregion

        #region jacoco maven files
        public static readonly string PomWithJacocoCCXml =
            @"<project xmlns = 'http://maven.apache.org/POM/4.0.0' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xsi:schemaLocation='http://maven.apache.org/POM/4.0.0 http://maven.apache.org/maven-v4_0_0.xsd'>
              <modelVersion>4.0.0</modelVersion>
              <groupId>com.mycompany.app</groupId>
              <artifactId>my-app</artifactId>
              <packaging>jar</packaging>
              <version>1.0-SNAPSHOT</version>
              <name>my-app</name>
              <url>http://maven.apache.org</url>  
              <build>
                <pluginManagement>
                  <plugins>
                    <plugin>
                      <groupId>org.jacoco</groupId>
                      <artifactId>jacoco-maven-plugin</artifactId>
                      <version>0.7.6-SNAPSHOT</version>

                      <executions>
                        <execution>
                          <id>pre-unit-test</id>
                          <goals>
                            <goal>prepare-agent</goal>
                          </goals>
                          <configuration>
                            <destFile>jacoco1.exec</destFile>
                            <propertyName>surefireArgLine</propertyName>
                          </configuration>
                        </execution>

                        <execution>
                          <id>post-unit-test</id>
                          <phase>test</phase>
                          <goals>
                            <goal>report</goal>
                          </goals>
                          <configuration>
                            <dataFile>jacoco1.exec</dataFile>
                            <outputDirectory>${project.reporting.outputDirectory
                }/jacoco-ut</outputDirectory>
                          </configuration>
                        </execution>

                      </executions>
                    </plugin>
                  </plugins>
                </pluginManagement>
                <plugins>
                  <plugin>
                    <groupId>org.jacoco</groupId>
                    <artifactId>jacoco-maven-plugin</artifactId>
                    <version>0.7.6-SNAPSHOT</version>

                    <executions>
                      <execution>
                        <id>pre-unit-test</id>
                        <goals>
                          <goal>prepare-agent</goal>
                        </goals>
                        <configuration>
                          <destFile>jacoco1.exec</destFile>
                          <propertyName>surefireArgLine</propertyName>
                        </configuration>
                      </execution>

                      <execution>
                        <id>post-unit-test</id>
                        <phase>test</phase>
                        <goals>
                          <goal>report</goal>
                        </goals>
                        <configuration>
                          <dataFile>jacoco1.exec</dataFile>
                          <outputDirectory>${project.reporting.outputDirectory
            }/jacoco-ut</outputDirectory>
                        </configuration>
                      </execution>

                    </executions>
                  </plugin>
                </plugins>
              </build>
              <dependencies>
                <dependency>
                  <groupId>junit</groupId>
                  <artifactId>junit</artifactId>
                  <version>3.8.1</version>
                  <scope>test</scope>
                </dependency>
              </dependencies>
            </project>";

        public static readonly string PomWithMultiModuleWithCCJacocoXml =
            @"<?xml version='1.0' encoding='utf-8'?>
            <project xmlns = 'http://maven.apache.org/POM/4.0.0' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xsi:schemaLocation='http://maven.apache.org/POM/4.0.0 http://maven.apache.org/maven-v4_0_0.xsd'>  
              <parent>
                <modelVersion>2.0.0</modelVersion>
                <groupId>com.mycompany.app</groupId>
                <artifactId>my-app</artifactId>
                <version>1.0-SNAPSHOT</version>
              </parent>
  
              <packaging>pom</packaging>
              <name>my-app</name>
              <url>http://maven.apache.org</url>
              <modules>
                <module>module-1</module>
                <module>module-2</module>
              </modules>
              <build>
                <plugins>
                  <plugin>
                    <groupId>org.jacoco</groupId>
                    <artifactId>jacoco-maven-plugin</artifactId>
                    <version>0.7.6-SNAPSHOT</version>

                    <executions>
                      <execution>
                        <id>pre-unit-test</id>
                        <goals>
                          <goal>prepare-agent</goal>
                        </goals>
                        <configuration>
                          <destFile>jacoco1.exec</destFile>
                          <propertyName>surefireArgLine</propertyName>
                        </configuration>
                      </execution>

                      <execution>
                        <id>post-unit-test</id>
                        <phase>test</phase>
                        <goals>
                          <goal>report</goal>
                        </goals>
                        <configuration>
                          <dataFile>jacoco1.exec</dataFile>
                          <outputDirectory>${project.reporting.outputDirectory
                }/jacoco-ut</outputDirectory>
                        </configuration>
                      </execution>

                    </executions>
                  </plugin>
                </plugins>
              </build>
              <dependencies>
                <dependency>
                  <groupId>junit</groupId>
                  <artifactId>junit</artifactId>
                  <version>3.8.1</version>
                  <scope>test</scope>
                </dependency>
              </dependencies>
            </project>";

        public static readonly string PomWithInvalidCCXml =
            @"<project xmlns = 'http://maven.apache.org/POM/4.0.0' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xsi:schemaLocation='http://maven.apache.org/POM/4.0.0 http://maven.apache.org/maven-v4_0_0.xsd'>
              <modelVersion>4.0.0</modelVersion>
              <groupId>com.mycompany.app</groupId>
              <artifactId>my-app</artifactId>
              <packaging>jar</packaging>
              <version>1.0-SNAPSHOT</version>
              <name>my-app</name>
              <url>http://maven.apache.org</url>
              <build>
                <plugins>
                  <plugin>
                    <groupId>org.jacoco</groupId>
                    <version>0.7.6-SNAPSHOT</version>
                    <configuration>
                      <destFile>codeCoverage\jacocoexec.exec</destFile>
                      <outputDirectory>codeCoverage</outputDirectory>
                      <dataFile>codeCoverage\jacocoexec.exec</dataFile>
                      <excludes>
                        <exclude>com.mycompany.app.App2*</exclude>
                      </excludes>
                      <includes />
                    </configuration>
                    <executions>
                      <execution>
                        <id>default-prepare-agent</id>
                        <goals>
                          <goal>prepare-agent</goal>
                        </goals>
                      </execution>
                      <execution>
                        <id>default-report1</id>
                        <phase>test</phase>
                        <goals>
                          <goal>report</goal>
                        </goals>
                      </execution>
                    </executions>
                  </plugin>
                </plugins>
              </build>
              <dependencies>
                <dependency>
                  <groupId>junit</groupId>
                  <artifactId>junit</artifactId>
                  <version>3.8.1</version>
                  <scope>test</scope>
                </dependency>
              </dependencies>
            </project>";
        #endregion
    }
}
