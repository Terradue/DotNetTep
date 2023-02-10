
pipeline {
  environment {
      CONFIGURATION = getConfiguration(env.BRANCH_NAME)
  }
  agent { node { label 'docker' } }
  stages {
    stage('Init') {
      steps {
        sh 'rm -rf packges */bin build'
        sh 'mkdir -p build'
        sh 'ls -la'
      }
    }
    stage('Build & Publish'){
      agent { node { label 'centos7-mono4' } }
      stages {
        stage('Build') {
          steps {
            echo "The library will be build in ${env.CONFIGURATION}"
            sh "msbuild /t:build /p:Configuration=${env.CONFIGURATION} /restore:True"
          }
        }
        stage('Test'){
          when {
            branch 'test'
          }
          steps {
            sh 'mono packages/nunit.consolerunner/3.10.0/tools/nunit3-console.exe *.Test/bin/*/net4*/*.Test.dll'
            nunit(testResultsPattern: 'TestResult.xml')
          }
        }
        stage('Package') {
          steps {
            sh "msbuild /t:pack /p:Configuration=${env.CONFIGURATION}"
            sh 'cat */obj/*/*.nuspec'           
          }
        }
        stage('Publish NuGet') {
          when {
            branch 'master'
          }
          steps {
            withCredentials([string(credentialsId: 'nuget_token', variable: 'NUGET_TOKEN')]) {
              echo 'Deploying'
              sh "nuget push build/Terradue.Tep*.nupkg -ApiKey ${NUGET_TOKEN} -SkipDuplicate -Source https://www.nuget.org/api/v2/package"          
            }
          }        
        }
      }
    }
  }
}

def getConfiguration(branchName) {
  def matcher = (branchName =~ /master/)
  if (matcher.matches())
    return "Release"
  
  return "Debug"
}