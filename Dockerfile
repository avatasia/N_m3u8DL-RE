# 使用官方的 .NET SDK 镜像
FROM mcr.microsoft.com/dotnet/sdk:9.0

# 备份原有的 sources.list 文件，并配置新的清华镜像源
RUN mv /etc/apt/sources.list.d/debian.sources /etc/apt/sources.list.d/debian.sources.disabled && \
    echo "deb https://mirrors.tuna.tsinghua.edu.cn/debian/ bookworm main contrib non-free" > /etc/apt/sources.list && \
    echo "deb https://mirrors.tuna.tsinghua.edu.cn/debian-security bookworm-security main contrib non-free" >> /etc/apt/sources.list && \
    echo "deb https://mirrors.tuna.tsinghua.edu.cn/debian/ bookworm-updates main contrib non-free" >> /etc/apt/sources.list && \
    echo "deb https://mirrors.tuna.tsinghua.edu.cn/debian/ bookworm-backports main contrib non-free" >> /etc/apt/sources.list

# 更新包源并安装 clang 和中文支持
RUN apt-get update && apt-get install -y clang locales locales-all

# 设置中文语言环境
RUN locale-gen zh_CN.UTF-8 
RUN update-locale LANG=zh_CN.UTF-8 LC_ALL=zh_CN.UTF-8

# 设置环境变量
ENV LANG=zh_CN.UTF-8
ENV LC_ALL=zh_CN.UTF-8

# 暴露/app目录作为工作目录
WORKDIR /app

# 启动容器时默认执行 bash shell
CMD ["bash"]
