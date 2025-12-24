# 推送到GitHub并自动编译Windows exe

## 快速步骤

### 1. 创建GitHub仓库

1. 访问 https://github.com/new
2. 仓库名称：`ca210-white-balance`
3. 设为**Private**（私有）或Public（公开）
4. **不要**勾选"Add a README file"
5. 点击"Create repository"

### 2. 推送代码到GitHub

```bash
# 进入项目目录
cd /Users/wangguangyang/Desktop/AI\ code/CA210上位机软件/CA210WhiteBalance

# 添加远程仓库（替换YOUR_USERNAME为你的GitHub用户名）
git remote add origin https://github.com/YOUR_USERNAME/ca210-white-balance.git

# 推送代码
git branch -M main
git push -u origin main
```

### 3. 等待自动编译完成

1. 访问你的GitHub仓库页面
2. 点击顶部的 **"Actions"** 标签
3. 你会看到"Build Windows Application"工作流正在运行
4. 等待几分钟，绿色的✅表示编译成功

### 4. 下载编译好的Windows程序

1. 在Actions页面，点击最新的工作流运行
2. 滚动到底部的 **"Artifacts"** 部分
3. 下载 `CA210WhiteBalance-win-x64-zip`
4. 解压zip文件，里面有 `CA210WhiteBalance.exe`

---

## 如果需要手动触发编译

1. 进入仓库的 **Actions** 页面
2. 选择 **"Build Windows Application"**
3. 点击右侧的 **"Run workflow"** 按钮
4. 选择分支（main），点击绿色的 **"Run workflow"**

---

## 注意事项

由于项目使用了CA-SDK COM组件，第一次编译可能会遇到以下情况：

### COM引用警告

```
warning MSB3277: Found COM reference 'CA200Srvr'
```

这是正常的，可以忽略。GitHub Actions的Windows环境会处理这个引用。

### 如果编译失败

1. 在项目仓库中创建一个 `build.patch` 文件来修复COM引用问题
2. 或者修改 `.github/workflows/build.yml` 添加条件编译

---

## 替代方案：手动在GitHub网页上上传

如果git push命令不工作：

1. 在GitHub上创建仓库后，点击"uploading an existing file"
2. 将整个 `CA210WhiteBalance` 文件夹压缩为zip
3. 上传并提交
4. Actions会自动触发编译
