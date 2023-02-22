pipeline {
  agent { node { label 'docker' } }
  environment {
      VERSION_LIB = getVersionFromCsProj('Terradue.Tep/Terradue.Tep.csproj')
      VERSION_TYPE = getTypeOfVersion(env.BRANCH_NAME)
      CONFIGURATION = getConfiguration(env.BRANCH_NAME)
      JENKINS_API_TOKEN = credentials('jenkins_api_token_repository')      
  }
  stages {
    stage('.Net Core') {
      agent {           
          dockerfile {
            additionalBuildArgs '-t dotnet/sdk-mono-tep:6.0'
            additionalBuildArgs "--build-arg JENKINS_API_TOKEN=${env.JENKINS_API_TOKEN}"
          }
      }
      environment {
        DOTNET_CLI_HOME = "/tmp/DOTNET_CLI_HOME"
      }
      stages {
        stage("Build & Test") {
          steps {
            echo "Build .NET application"
            sh "dotnet restore ./"
            sh "dotnet build -c ${env.CONFIGURATION} --no-restore ./"
            sh "dotnet test -c ${env.CONFIGURATION} --no-build --no-restore ./"
          }
        }
        stage('Publish NuGet') {
          when{
            branch pattern: "(release\\/[\\d.]+|master)", comparator: "REGEXP"
          }
          steps {
            withCredentials([string(credentialsId: 'nuget_token', variable: 'NUGET_TOKEN')]) {
              sh "dotnet pack Terradue.Tep/Terradue.Tep.csproj -c ${env.CONFIGURATION} -o publish"
              sh "dotnet nuget push publish/*.nupkg --skip-duplicate -k $NUGET_TOKEN -s https://api.nuget.org/v3/index.json"
            }
          }
        }
      }
    }
  }
}

def getTypeOfVersion(branchName) {
  def matcher = (branchName =~ /(v[\d.]+|release\/[\d.]+|master)/)
  if (matcher.matches())
    return ""
  
  return "dev"
}

def getConfiguration(branchName) {
  def matcher = (branchName =~ /(release\/[\d.]+|master)/)
  if (matcher.matches())
    return "Release"
  
  return "Debug"
}

def getVersionFromCsProj (csProjFilePath){
  def file = readFile(csProjFilePath) 
  def xml = new XmlSlurper().parseText(file)
  def suffix = ""
  if ( xml.PropertyGroup.VersionSuffix[0].text() != "" )
    suffix = "-" + xml.PropertyGroup.VersionSuffix[0].text()
  return xml.PropertyGroup.Version[0].text() + suffix
}
