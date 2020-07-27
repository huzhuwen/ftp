/// <summary>
/// 文件信息
/// </summary>
public struct FtpFileInfo
{
    public string FileName;
    public bool IsFolder;
}
/// <summary>
/// ftp功能接口
/// </summary>
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
    /// <summary>
    /// 地址
    /// </summary>
    string M_ServerIP { get; }
}
/// <summary>
/// ftp功能类
/// </summary>
public class FTPFiles : INetFiles
{
    #region 属性
    /// <summary>
    /// 服务器ip  
    /// </summary>
    private string _ftpServerIP;
    /// <summary>
    /// 服务器ip（地址）
    /// </summary>
    public string M_ServerIP { get { return _ftpServerIP; } }
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
                    //这里，文件的read方法，是把数据写入到data中。
                    //这data指针是不会变的。所以数组永远是只有这一个
                    //由于下在的write，是把data写入，
                    //所以这个write文件自身数据节点是一直往下延的，
                    //所以data用不着重新转成新数组（如toArray()，【新的变量定义，指针不同】）
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
                ftpStream.CopyTo(file);//文件流之间的转换。这个在网上看到
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
    /// <returns>
    /// 由于历史原因，这个方法要开放出来。
    /// 同时发现，读大文件时，ftp自身的流好象总会出现错误。
    /// 要么读不到结束点，要么在不该结束的地方结束了
    /// 所以只能参照上面，流复制。
    /// 先产生一个文件，然后读这个文件，把文件转成byte[]集。
    /// </returns>
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
                ftpStream.CopyTo(w);//先创建一个文件
                w.Close();
                FileStream r = File.OpenRead(serviceFileName);//读这个文件
                do
                {
                    isend = r.Read(data, 0, FTPFiles.READ_LENGTH);
                    fileData.Add(data.ToArray());//由于数组自身指针不动，所以一定要把该数组创建一个新数组
                } while (isend == FTPFiles.READ_LENGTH);
                r.Close();
                File.Delete(serviceFileName);//删除该文件
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
/// <summary>
/// ftp应用类
/// </summary>
public class FTPFiles
{
    /// <summary>
    /// ftp的ip地址（初始登录地址）,ftp账号，密码，加密钥匙
    /// </summary>
    private readonly string _ip, _user, _pwd, _key;
    /// <summary>
    /// ftp的ip地址（初始登录地址）
    /// </summary>
    public string M_FtpIP { get { return _ip; } }
    /// <summary>
    /// ftp应用类对象
    /// </summary>
    private ftp.INetFiles _ftpInstance;
    /// <summary>
    /// ftp使用类
    /// </summary>
    public FTPFiles()
    {
        _key = Bases.ReadConfig.Instance.UserKey;
        _ip = zs.Security.DecryptDES(Bases.ReadConfig.Instance.FTP_IP, _key);
        _user = zs.Security.DecryptDES(Bases.ReadConfig.Instance.FTP_ID, _key);
        _pwd = zs.Security.DecryptDES(Bases.ReadConfig.Instance.FTP_Pwd, _key);
        _ftpInstance = new ftp.FTPFiles(_ip, _user, _pwd);
    }
    /// <summary>
    /// 设置ftp的ip（地址）
    /// </summary>
    /// <param name="ip"></param>
    public void SetFtpIp(string ip)
    {
        _ftpInstance.SetIPAddress(ip);
    }
    /// <summary>
    /// 排除一些文件（夹）
    /// </summary>
    public string[] M_ExceptFiles { get; set; }
    /// <summary>
    /// 下载文件
    /// </summary>
    /// <param name="servicesFileName"></param>
    /// <param name="currentFileName"></param>
    public void DownLoadFile(string servicesFileName, string currentFileName)
    {
        try
        {
            _ftpInstance.Download(currentFileName, servicesFileName);
        }
        catch
        {
            throw;
        }
    }
    /// <summary>
    /// 上传文件
    /// </summary>
    /// <param name="servicesFileName"></param>
    /// <param name="currentFileName"></param>
    public void UploadFile(string servicesFileName, string currentFileName)
    {
        try
        {
            _ftpInstance.Upload(currentFileName, servicesFileName);
        }
        catch
        {
            throw;
        }
    }
    /// <summary>
    /// 得到文件集
    /// </summary>
    public IEnumerable<FtpFileInfo> M_Files { get; set; }
    /// <summary>
    /// 得到所有服务器上文件
    /// 只要一个初始化ip（地址），下层的文件夹，文件等一次性全读出来
    /// </summary>
    /// <returns></returns>
    public void GetServiceAllFiles()
    {
        M_Files = (from aPath in GetServiceAllFiles(_ip)
                   where !string.IsNullOrWhiteSpace(aPath.FileName)
                   select aPath);
    }
    /// <summary>
    /// 得到所有的服务器上文件
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    private IEnumerable<FtpFileInfo> GetServiceAllFiles(string path)
    {
        FtpFileInfo afile;
        afile.FileName = string.Empty;
        afile.IsFolder = false;
        _ftpInstance.SetIPAddress(path);
        IList<string> files = ExceptFilesToFind(_ftpInstance.GetFiles());
        if (files == null || files.Count == 0)
        {
            yield break;
        }
        else
        {
            foreach (string one in files)
            {
                afile.FileName = (path + "/" + one);
                afile.IsFolder = io.Directory.Exists(string.Format("\\\\{0}", (_ftpInstance.M_ServerIP + "/" + afile.FileName).Replace('/', '\\')));
                yield return afile;
                foreach (FtpFileInfo sub in GetServiceAllFiles(afile.FileName))
                {
                    yield return sub;
                }
            }
        }
    }
    /// <summary>
    /// 排除一些文件（夹）
    /// </summary>
    /// <param name="readFiles"></param>
    /// <returns></returns>
    private IList<string> ExceptFilesToFind(string[] readFiles)
    {
        if (M_ExceptFiles == null || M_ExceptFiles.Length == 0
            || readFiles == null || readFiles.Length == 0)
        {
            return readFiles;
        }
        //不排序了。因为中文排序（拼音排序）。对应的(类ASCII码)没有排序，所以中文排序仍是乱的
        //return readFiles.Except(M_ExceptFiles).OrderBy(f => f).ToList();
        return readFiles.Except(M_ExceptFiles).ToList();
    }
    /// <summary>
    /// 得到服务器上某层文件
    /// 设置地址，SetFtpPath方法
    /// 读到的这一层文件夹（文件）名
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public void GetServiceOneLevelFiles()
    {
        IList<string> files = ExceptFilesToFind(_ftpInstance.GetFiles());
        IList<FtpFileInfo> list = new List<FtpFileInfo>();
        FtpFileInfo afile;
        foreach (string one in files)
        {

            afile.FileName = one;
            afile.IsFolder = io.Directory.Exists(string.Format("\\\\{0}", (_ftpInstance.M_ServerIP + "/" + one).Replace('/', '\\')));
            list.Add(afile);
        }
        M_Files = list;
    }
    /// <summary>
    /// 设置地址
    /// </summary>
    /// <param name="path"></param>
    public void SetFtpPath(string path)
    {
        _ftpInstance.SetIPAddress(path);
    }
}
