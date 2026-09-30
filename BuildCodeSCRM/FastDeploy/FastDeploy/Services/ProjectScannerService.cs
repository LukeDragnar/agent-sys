using FastDeploy.Common;
using FastDeploy.Models;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace FastDeploy.Services
{
    public class ProjectScannerService
    {
        public void SetCachePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            string appBasePath = AppContext.BaseDirectory;
            var xmlPath = Path.Combine(appBasePath, "Data", "SearchPath.xml");

            // Ensure Data folder exists
            string dataDir = Path.GetDirectoryName(xmlPath)!;
            if (!Directory.Exists(dataDir))
            {
                Directory.CreateDirectory(dataDir);
            }

            XDocument doc;

            if (!File.Exists(xmlPath))
            {
                doc = new XDocument(
                    new XDeclaration("1.0", "utf-8", "yes"),
                    new XElement("Config",
                        new XElement("LastPath", path)
                    )
                );
            }
            else
            {
                doc = XDocument.Load(xmlPath);

                var root = doc.Element("Config");
                if (root == null)
                {
                    root = new XElement("Config");
                    doc.Add(root);
                }

                var lastPath = root.Element("LastPath");
                if (lastPath == null)
                {
                    root.Add(new XElement("LastPath", path));
                }
                else
                {
                    lastPath.Value = path;
                }
            }

            doc.Save(xmlPath);
        }

        public string GetCachePath()
        {
            string appBasePath = AppContext.BaseDirectory;
            var xmlPath = Path.Combine(appBasePath, "Data", "SearchPath.xml");

            if (!File.Exists(xmlPath))
                return string.Empty;

            var doc = XDocument.Load(xmlPath);

            return doc.Element("Config")
                      ?.Element("LastPath")
                      ?.Value ?? string.Empty;
        }

        public List<ProjectInfo> ScanApiProjects(string env, string rootPath)
        {
            return Directory.GetDirectories(rootPath, "*API*", SearchOption.AllDirectories)
                .Select(dir =>
                {
                    var batFiles = Directory.GetFiles(dir, "*.bat", SearchOption.TopDirectoryOnly);
                    var appSettings = Directory.GetFiles(dir, "appsettings*.json", SearchOption.TopDirectoryOnly);

                    var batPath = string.Empty;
                    switch (env)
                    {
                        case EnumEnviroment.STAGING:
                            batPath = batFiles.FirstOrDefault(f => f.Contains("staging", StringComparison.OrdinalIgnoreCase));
                            break;
                        case EnumEnviroment.PRODUCTION:
                            batPath = batFiles.FirstOrDefault(f => f.Contains("production", StringComparison.OrdinalIgnoreCase));
                            break;
                        default:
                            break;
                    }

                    return new ProjectInfo
                    {
                        ProjectName = Path.GetFileName(dir),
                        ProjectPath = dir,
                        BatPath = batPath,
                    };
                })
                .Where(p => p.BatPath != null)
                .ToList();
        }

        public void HandleSaveBat(DataGridViewRow row)
        {
            //var row = lstSelectedProject.Rows[rowIndex];
            if (row == null) return;

            var batPath = row.Cells["colPath"].Value?.ToString();
            if (string.IsNullOrWhiteSpace(batPath) || !File.Exists(batPath))
            {
                MessageBox.Show("Bat file không tồn tại");
                return;
            }

            var curVersion = row.Cells["colCurVersion"].Value?.ToString();
            var newVersion = row.Cells["colNewVersion"].Value?.ToString();

            if (string.IsNullOrWhiteSpace(curVersion) || string.IsNullOrWhiteSpace(newVersion))
            {
                MessageBox.Show("Version không hợp lệ");
                return;
            }

            // Đọc toàn bộ file bat
            var content = File.ReadAllText(batPath);

            // Replace version cũ -> version mới
            content = ReplaceVersion(content, curVersion, newVersion);

            row.Cells["colCurVersion"].Value = newVersion;
            row.Cells["colNewVersion"].Value = FunctionCommon.UpVersion(newVersion);

            // Ghi lại file
            File.WriteAllText(batPath, content);
        }

        public void HandleDeploy(DataGridViewRow row)
        {
            //var row = lstSelectedProject.Rows[rowIndex];
            if (row == null) return;

            var batPath = row.Cells["colPath"].Value?.ToString();
            if (string.IsNullOrWhiteSpace(batPath))
            {
                MessageBox.Show("Đường dẫn file bat rỗng!", "Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!File.Exists(batPath))
            {
                MessageBox.Show($"Không tìm thấy file:\n{batPath}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                System.Diagnostics.Process.Start(new ProcessStartInfo
                {
                    FileName = batPath,
                    WorkingDirectory = Path.GetDirectoryName(batPath)!,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Open file failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string ReplaceVersion(string content, string oldVersion, string newVersion)
        {
            return Regex.Replace(
                content,
                $@"(?<!\d){Regex.Escape(oldVersion)}(?!\d)",
                newVersion
            );
        }

        public string LoadPortainerFromXml(string env, string projectName)
        {
            string appBasePath = AppContext.BaseDirectory;
            var xmlPath = string.Empty;
            if (env == EnumEnviroment.STAGING)
            {
                xmlPath = Path.Combine(appBasePath, "Data", "PortainerStaging.xml");
            } else if (env == EnumEnviroment.PRODUCTION)
            {
                xmlPath = Path.Combine(appBasePath, "Data", "PortainerProduction.xml");
            } else
            {
                return string.Empty;
            }
            string dataDir = Path.GetDirectoryName(xmlPath)!;
            if (!Directory.Exists(dataDir))
            {
                Directory.CreateDirectory(dataDir);
            }

            // 2️⃣ Tạo file XML nếu chưa có
            if (!File.Exists(xmlPath))
            {
                var root = new XElement("Projects");
                root.Add(new XComment("Projects config - auto generated"));

                var newDoc = new XDocument(
                    new XDeclaration("1.0", "utf-8", "yes"),
                    root
                );

                newDoc.Save(xmlPath);
                return string.Empty;
            }

            var doc = XDocument.Load(xmlPath);

            var project = doc.Descendants("Project")
                .FirstOrDefault(p =>
                    string.Equals(
                        p.Element("Name")?.Value,
                        projectName,
                        StringComparison.OrdinalIgnoreCase));

            return project?
                .Element("Portainer")?
                .Element("Url")?
                .Value ?? string.Empty;
        }

        public void SavePoitanerFromXml(string env, string projectName, string newUrl)
        {
            string appBasePath = AppDomain.CurrentDomain.BaseDirectory;
            var xmlPath = string.Empty;
            if (env == EnumEnviroment.STAGING)
            {
                xmlPath = Path.Combine(appBasePath, "Data", "PortainerStaging.xml");
            }
            else if (env == EnumEnviroment.PRODUCTION)
            {
                xmlPath = Path.Combine(appBasePath, "Data", "PortainerProduction.xml");
            }
            else
            {
                return;
            }

            if (!File.Exists(xmlPath))
            {
                return;
            }


            var doc = XDocument.Load(xmlPath);

            var projectsRoot = doc.Element("Projects");
            if (projectsRoot == null)
                return;

            // 1️⃣ Tìm Project theo Name
            var project = projectsRoot.Elements("Project")
                .FirstOrDefault(p =>
                    string.Equals(
                        p.Element("Name")?.Value,
                        projectName,
                        StringComparison.OrdinalIgnoreCase));

            // 2️⃣ Nếu chưa có Project → tạo mới
            if (project == null)
            {
                project = new XElement("Project",
                    new XElement("Name", projectName),
                    new XElement("Portainer",
                        new XElement("Stack", projectName), // hoặc truyền Stack riêng
                        new XElement("Url", newUrl)
                    )
                );

                projectsRoot.Add(project);
            }
            else
            {
                // 3️⃣ Đã có Project → xử lý Portainer
                var portainer = project.Element("Portainer");
                if (portainer == null)
                {
                    portainer = new XElement("Portainer");
                    project.Add(portainer);
                }

                // 4️⃣ Update Stack (optional)
                var stackElement = portainer.Element("Stack");
                if (stackElement == null)
                {
                    stackElement = new XElement("Stack", projectName);
                    portainer.Add(stackElement);
                }

                // 5️⃣ Update Url
                var urlElement = portainer.Element("Url");
                if (urlElement == null)
                {
                    portainer.Add(new XElement("Url", newUrl));
                }
                else
                {
                    urlElement.Value = newUrl;
                }
            }

            doc.Save(xmlPath);
        }
    }
}
