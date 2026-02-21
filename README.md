# CloudFormationStackDeployer
Deploy CloudFormation stacks by chaining their outputs and inputs

# Install the tool
## Download .NET SDK
Visit https://dot.net/core

## Install the tool
```sh
dotnet tool install -g MbUtils.CloudFormationStackDeployer
```

# Sample project file
```yaml
stackPrefix: sample-app-
variables:
  bucketName: molnarbence-sample-storage
  project: SampleProject

stackTags:
  Contact: myemail@example.com
  Project: ${variables.project}

bucketsToEmpty:
  - ${variables.bucketName}

stacks:
  - name: iam-role
    template: templates/iam-role.yml
    parameters:
      RoleName: sample-app-role
      BucketName: ${variables.bucketName}
      Project: ${variables.project}
  - name: bucket
    template: templates/bucket.yml
    parameters:
      BucketName: ${variables.bucketName}
      RoleARN: ${outputs.iam-role.RoleARN}
      Project: ${variables.project}
  
```

# Available commands
## Deploy stacks
```sh
cf-deploy deploy
```
Then choose the stack from the list offered by the tool.

## Delete stacks
```sh
cf-deploy delete
```
Then choose the stack from the list offered by the tool.

## Empty bucket
```sh
cf-deploy empty-bucket
```
Then choose the bucket from the list offered by the tool.