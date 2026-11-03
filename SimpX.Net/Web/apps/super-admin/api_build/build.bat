@echo off
CHCP 65001

set dir=%~dp0

set moduleName=system
set apiServicesPath=%dir%..\src\api-services\system\
set apiUrl=http://localhost:5005/swagger/Default/swagger.json

if "%1"=="jeeFlow" (
  set moduleName=jeeFlow
  set apiServicesPath=%dir%..\src\api-services\jeeFlow\
  set apiUrl=http://localhost:5005/swagger/JeeFlow/swagger.json
) else if "%1"=="media" (
  set moduleName=media
  set apiServicesPath=%dir%..\src\api-services\media\
  set apiUrl=http://localhost:5005/swagger/Media/swagger.json
) else if "%1"=="viewFace" (
  set moduleName=viewFace
  set apiServicesPath=%dir%..\src\api-services\viewFace\
  set apiUrl=http://localhost:5005/swagger/ViewFace/swagger.json
) else if "%1"=="document" (
  set moduleName=document
  set apiServicesPath=%dir%..\src\api-services\document\
  set apiUrl=http://localhost:5005/swagger/Document/swagger.json
) else if "%1"=="workWeixin" (
  set moduleName=workWeixin
  set apiServicesPath=%dir%..\src\api-services\workWeixin\
  set apiUrl=http://localhost:5005/swagger/WorkWeixin/swagger.json
) else if "%1"=="dingTalk" (
  set moduleName=dingTalk
  set apiServicesPath=%dir%..\src\api-services\dingTalk\
  set apiUrl=http://localhost:5005/swagger/DingTalk/swagger.json
) else if "%1"=="goView" (
  set moduleName=goView
  set apiServicesPath=%dir%..\src\api-services\goView\
  set apiUrl=http://localhost:5005/swagger/GoView/swagger.json
) else if "%1"=="agent" (
  set moduleName=agent
  set apiServicesPath=%dir%..\src\api-services\agent\
  set apiUrl=http://localhost:5005/swagger/Agent/swagger.json
) else if "%1"=="paddleOCR" (
  set moduleName=paddleOCR
  set apiServicesPath=%dir%..\src\api-services\paddleOCR\
  set apiUrl=http://localhost:5005/swagger/PaddleOCR/swagger.json
) else if "%1"=="main" (
  set moduleName=main
  set apiServicesPath=%dir%..\src\api-services\main\
  set apiUrl=http://localhost:5005/swagger/main/swagger.json
)

if exist %apiServicesPath% (
    echo ================================ 删除目录 %moduleName% ================================
    rd /s /q %apiServicesPath%
)

echo ================================ 开始生成 %moduleName% ================================

java -jar %dir%swagger-codegen-cli.jar generate -i %apiUrl% -l typescript-axios -o %apiServicesPath%

@rem 删除不必要的文件和文件夹
rd /s /q %apiServicesPath%.swagger-codegen
del /q %apiServicesPath%.gitignore
del /q %apiServicesPath%.npmignore
del /q %apiServicesPath%.swagger-codegen-ignore
del /q %apiServicesPath%git_push.sh
del /q %apiServicesPath%package.json
del /q %apiServicesPath%README.md
del /q %apiServicesPath%tsconfig.json

echo ================================ 生成结束 ================================