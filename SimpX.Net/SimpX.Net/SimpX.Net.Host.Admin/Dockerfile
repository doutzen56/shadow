FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 5005

COPY . .

# 设置语言/区域设置环境变量
ENV LANG zh-Hans

# 网络慢时可以把 deb.debian.org 换成就近的软件源
# 更新包管理器并安装free命令
RUN apt-get update && apt-get install -y procps

ENTRYPOINT ["dotnet", "SimpX.Net.Host.Admin.dll"]