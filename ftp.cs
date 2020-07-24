public interface INetFiles
    {
        /// <summary>
        /// 设置地址
        /// </summary>
        /// <param name="ipAddress"></param>
        void SetIPAddress(string ipAddress);
        /// <summary>
        /// 本地文件上传，所有一般只用在window项目中
        /// </summary>
        /// <param name="pCurrentFileFullName">本地文件名（含路径）</param>
        /// <param name="pServiceFileName">服务器上的文件名</param>
        void Upload(string currentFileFullName, string serviceFileName);
        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="pCurrentFileFullName">本地要产生的文件名（含路径）</param>
        /// <param name="pServiceFileName">服务器上的文件名</param>
        void Download(string currentFileFullName, string serviceFileName);
        /// <summary>
        /// 读取FTP服务器上文件
        /// </summary>
        /// <param name="pServiceFileName">服务器文件名</param>
        /// <returns></returns>
        IList<byte[]> ReadServiceFile(string serviceFileName);
        /// <summary>
        /// 得到FTP服务器上的文件
        /// </summary>
        /// <returns></returns>
        string[] GetFiles();
    }
    public class FTPFiles : INetFiles
    {
        #region 属性
        /// <summary>
        /// 服务器ip  
        /// </summary>
        private string _ftpServerIP;
        /// <summary>
        /// 用户名,密码
        /// </summary>
        private readonly string _ftpUserID, _ftpPassword;
        /// <summary>
        /// 读取文件的分组大小（每3072个字节读一次）
        /// </summary>
        private const int READ_LENGTH = 3072;
        #endregion 属性

        public FTPFiles(string serviceIP, string ftpUser, string ftpPassword)
        {
            _ftpPassword = ftpPassword;
            _ftpServerIP = serviceIP;
            _ftpUserID = ftpUser;
        }
        /// <summary>
        /// 创建服务器上某个文件的ftp请求
        /// </summary>
        /// <param name="serviceFileName"></param>
        private FtpWebRequest CreateFtpRequest(string serviceFileName)
        {
            // 根据uri创建FtpWebRequest对象  
            FtpWebRequest req = FtpWebRequest.Create(new Uri("ftp://" + _ftpServerIP + "/" + serviceFileName)) as FtpWebRequest;
            if (req == null)
            {
                throw new Exception("ftp请求对象创建失败");
            }
            return req;
        }
        /// <summary>
        /// 创建服务器上某个文件的ftp服务器请求
        /// </summary>
        /// <param name="serviceFileName"></param>
        private FtpWebResponse CreateFtpResponse(FtpWebRequest req)
        {
            // 根据uri创建FtpWebRequest对象  
            FtpWebResponse response = req.GetResponse() as FtpWebResponse;
            if (response == null)
            {
                throw new Exception("ftp服务器请求创建失败");
            }
            return response;
        }
        /// <summary>
        /// ftp账号是否有空值
        /// </summary>
        /// <returns></returns>
        private bool CheckFtpAccountInfoIsNullOrEmpty()
        {
            bool flag = string.IsNullOrEmpty(_ftpUserID) || string.IsNullOrEmpty(_ftpPassword)
                    || string.IsNullOrWhiteSpace(_ftpUserID) || string.IsNullOrWhiteSpace(_ftpPassword)
                    || string.IsNullOrEmpty(_ftpServerIP) || string.IsNullOrWhiteSpace(_ftpServerIP);
            if (flag)
            {
                throw new Exception("ftp账号有空值");
            }
            return flag;
        }
        /// <summary>
        /// 上传某个文件
        /// </summary>
        /// <param name="currentFileFullName">本地文件</param>
        /// <param name="serviceFileName">服务器上文件名</param>
        public void Upload(string currentFileFullName, string serviceFileName = null)
        {
            int isend = 0;
            FileInfo fileInfo = new FileInfo(currentFileFullName);
            if (string.IsNullOrEmpty(serviceFileName) || string.IsNullOrWhiteSpace(serviceFileName))
            {
                serviceFileName = fileInfo.Name;
            }
            try
            {
                CheckFtpAccountInfoIsNullOrEmpty();
                // 根据uri创建FtpWebRequest对象  
                FtpWebRequest reqFTP = CreateFtpRequest(serviceFileName);
                System.Threading.Thread.Sleep(200);
                // ftp用户名和密码 
                reqFTP.Credentials = new NetworkCredential(_ftpUserID, _ftpPassword);
                // 默认为true，连接不会被关闭 
                // 在一个命令之后被执行        
                reqFTP.KeepAlive = false;
                // 指定执行什么命令 
                reqFTP.Method = WebRequestMethods.Ftp.UploadFile;
                // 指定数据传输类型 
                reqFTP.UseBinary = true;
                // 上传文件时通知服务器文件的大小 
                reqFTP.ContentLength = fileInfo.Length;
                FileStream fs = fileInfo.OpenRead();
                byte[] data = new byte[FTPFiles.READ_LENGTH];
                System.Threading.Thread.Sleep(200);
                // 把上传的文件写入流 
                using (Stream strm = reqFTP.GetRequestStream())
                {
                    do
                    {
                        isend = fs.Read(data, 0, FTPFiles.READ_LENGTH);
                        strm.Write(data, 0, isend);
                    } while (isend == FTPFiles.READ_LENGTH);
                }
                fs.Close();
                fs.Dispose();
            }
            catch (Exception ex)
            {
                TextLog.WriteLog(ex.ToString());
                throw;
            }
        }
        /// <summary>
        /// 下载某个文件（本地创建文件currentFileFullName）
        /// </summary>
        /// <param name="currentFileFullName">本地文件名</param>
        /// <param name="serviceFileName">服务器上文件名</param>
        public void Download(string currentFileFullName, string serviceFileName)
        {
            try
            {
                using (FileStream outputStream = new FileStream(currentFileFullName, FileMode.Create))
                {
                    ReadServiceFile(serviceFileName, outputStream);
                }
            }
            catch
            {
                throw;
            }
        }
        /// <summary>
        /// 读取某个文件
        /// </summary>
        /// <param name="serviceFileName"></param>
        /// <returns></returns>
        private IList<byte[]> ReadServiceFile(string serviceFileName, FileStream file)
        {
            IList<byte[]> fileData = new List<byte[]>();
            try
            {
                CheckFtpAccountInfoIsNullOrEmpty();
                FtpWebRequest reqFTP = CreateFtpRequest(serviceFileName);
                System.Threading.Thread.Sleep(200);
                reqFTP.Method = WebRequestMethods.Ftp.DownloadFile;
                reqFTP.UseBinary = true;
                // ftp用户名和密码 
                reqFTP.Credentials = new NetworkCredential(_ftpUserID, _ftpPassword);
                FtpWebResponse response = CreateFtpResponse(reqFTP);
                System.Threading.Thread.Sleep(200);
                using (Stream ftpStream = response.GetResponseStream())
                {
                    ftpStream.CopyTo(file);
                }
                response.Close();
            }
            catch (Exception ex)
            {
                TextLog.WriteLog(ex.ToString());
                throw;
            }
            return fileData;
        }
        /// <summary>
        /// 读取某个文件
        /// </summary>
        /// <param name="serviceFileName"></param>
        /// <returns></returns>
        public IList<byte[]> ReadServiceFile(string serviceFileName)
        {
            int isend = 0;
            IList<byte[]> fileData = new List<byte[]>();
            try
            {
                CheckFtpAccountInfoIsNullOrEmpty();
                FtpWebRequest reqFTP = CreateFtpRequest(serviceFileName);
                System.Threading.Thread.Sleep(200);
                reqFTP.Method = WebRequestMethods.Ftp.DownloadFile;
                reqFTP.UseBinary = true;
                // ftp用户名和密码 
                reqFTP.Credentials = new NetworkCredential(_ftpUserID, _ftpPassword);
                FtpWebResponse response = CreateFtpResponse(reqFTP);
                System.Threading.Thread.Sleep(200);
                byte[] data = new byte[FTPFiles.READ_LENGTH];
                using (Stream ftpStream = response.GetResponseStream())
                {
                    FileStream w = File.Create(serviceFileName);
                    ftpStream.CopyTo(w);
                    w.Close();
                    FileStream r = File.OpenRead(serviceFileName);
                    do
                    {
                        isend = r.Read(data, 0, FTPFiles.READ_LENGTH);
                        fileData.Add(data.ToArray());
                    } while (isend == FTPFiles.READ_LENGTH);
                    r.Close();
                    File.Delete(serviceFileName);
                }
                response.Close();
            }
            catch (Exception ex)
            {
                TextLog.WriteLog(ex.ToString());
                throw;
            }
            return fileData;
        }
        /// <summary>
        /// 从ftp服务器上获得文件列表
        /// </summary>
        /// <returns></returns>
        public string[] GetFiles()
        {
            return GetFiles(string.Empty);
        }
        /// <summary>
        /// 从ftp服务器上主目录下（ip代表路径）获得文件列表
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private string[] GetFiles(string path)
        {
            string[] downloadFiles = null;
            IList<string> files = new List<string>();
            try
            {
                CheckFtpAccountInfoIsNullOrEmpty();
                FtpWebRequest reqFTP = CreateFtpRequest(path);
                System.Threading.Thread.Sleep(200);
                reqFTP.UseBinary = true;
                reqFTP.KeepAlive = false;
                // ftp用户名和密码 
                reqFTP.Credentials = new NetworkCredential(_ftpUserID, _ftpPassword);
                reqFTP.Method = WebRequestMethods.Ftp.ListDirectory;
                FtpWebResponse response = CreateFtpResponse(reqFTP);
                System.Threading.Thread.Sleep(200);
                using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                {
                    string afile = reader.ReadLine();
                    while (!string.IsNullOrEmpty(afile))
                    {
                        files.Add(afile);
                        afile = reader.ReadLine();
                    }
                }
                response.Close();
                downloadFiles = files.ToArray();
            }
            catch (Exception ex)
            {
                TextLog.WriteLog(ex.ToString());
                downloadFiles = null;
            }
            return downloadFiles;
        }
        /// <summary>
        /// 设置地址（ip）
        /// </summary>
        /// <param name="ipAddress"></param>
        public void SetIPAddress(string ipAddress)
        {
            _ftpServerIP = ipAddress;
        }
    }
