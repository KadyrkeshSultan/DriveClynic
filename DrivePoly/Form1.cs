using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace DrivePoly
{
    public partial class Form1 : Form
    {
        static string[] Scopes = { DriveService.Scope.Drive };
        static string ApplicationName = "Drive API .NET Quickstart";
        public UserCredential credential;
        static bool StatusSelect = false;
        static string mimeType = string.Empty;

        public Form1()
        {
            InitializeComponent();

            using (var stream =
                new FileStream("client_secret.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = System.Environment.GetFolderPath(
                    System.Environment.SpecialFolder.Personal);
                credPath = Path.Combine(credPath, ".credentials/drive-dotnet-quickstart.json");

                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
            }
        }

        private void buttonUploadFile_Click(object sender, EventArgs e)
        {
            openFileDialog.ShowDialog();
            
            if (openFileDialog.FileName != "")
            {
                string fileExtension = openFileDialog.SafeFileName.Split('.')[1];
                labelFileName.Text = openFileDialog.SafeFileName;

                if (fileExtension == "docx" || fileExtension == "doc")
                {
                    mimeType = "application/msword";
                    StatusSelect = true;
                }
                else if (fileExtension == "xls" || fileExtension == "xlsx")
                {
                    mimeType = "application/vnd.ms-excel";
                    StatusSelect = true;
                }
                else
                {
                    MessageBox.Show("Выберите файл Word или Excel", "Уведомление");
                    return;
                }
            }
        }

        private void UploadFile(string mimeType)
        {
            try
            {
                var service = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName,
                });

                string idFolder = string.Empty;
                
                    var requestF = service.Files.List();
                    requestF.Q = "mimeType='application/vnd.google-apps.folder'";
                    requestF.Spaces = "drive";
                    requestF.Fields = "nextPageToken, files(id, name)";
                    var result = requestF.Execute();
                    foreach (var file in result.Files)
                    {
                        if(file.Name == textBoxName.Text)
                        {
                            idFolder = file.Id;
                            break;
                        }
                    }


                if (idFolder == string.Empty)
                {
                    //Создание папки
                    var folderMetadata = new Google.Apis.Drive.v3.Data.File()
                    {
                        Name = textBoxName.Text,
                        MimeType = "application/vnd.google-apps.folder"
                    };
                    var requestFolder = service.Files.Create(folderMetadata);
                    requestFolder.Fields = "id";
                    var folder = requestFolder.Execute();
                    idFolder = folder.Id;
                }

                Google.Apis.Drive.v3.Data.File fileMetadata = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = textBoxName.Text + " " + DateTime.Now,
                    Parents = new List<string>
                {
                    idFolder
                }
                };

                //Загрузка файла
                FilesResource.CreateMediaUpload request;
                using (var stream = new System.IO.FileStream(openFileDialog.FileName, FileMode.Open))
                {
                    request = service.Files.Create(fileMetadata, stream, mimeType);
                    request.Fields = "id";
                    request.Upload();
                    MessageBox.Show("Файл загружен", "Уведомление");
                }
            }
            catch
            {
                MessageBox.Show("Что-то пошло не так\nПопробуйте отправить позже", "Ошибка");
            }
        }

        private void buttonSendFile_Click(object sender, EventArgs e)
        {
            if (StatusSelect)
                UploadFile(mimeType);
            else
                MessageBox.Show("Вы не выбрали файл!", "Уведомление");
        }
    }
}
