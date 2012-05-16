<%@ Import Namespace="System.Text" %>
<%@ Import Namespace="System.IO" %>
<%@ Import Namespace="Ionic.Utils.Zip" %>


<script language="C#" runat="server">

  public bool showtitle = true;
  public String width = "100%";
  public String defaultTitle= "Zip Creator";
  private String font; 

public void Page_Load (Object sender, EventArgs e)
{
  try
    {
      if ( !Page.IsPostBack ) {
	// populate the dropdownlist
	String directoryName= "";
	// must have a directory called "source" in the web app 
	String sMappedPath= Server.MapPath("source/" +directoryName);

	String[] filenames = System.IO.Directory.GetFiles(sMappedPath);
	
	ListOfFiles.DataSource = filenames;
	ListOfFiles.DataBind();
      }

    }
  catch (Exception) 
    {
      // Ignored
    }
}


public void btnGo_Click (Object sender, EventArgs e)
{
  Response.Clear();
  String ReadmeText= "This is a zip file dynamically generated at " + System.DateTime.Now.ToString("G");
  string filename = System.IO.Path.GetFileName(ListOfFiles.SelectedItem.Text) + ".zip";
  Response.ContentType = "application/zip";
  Response.AddHeader("content-disposition", "filename=" + filename);
  
  using (ZipFile zip = new ZipFile(Response.OutputStream)) {
    zip.AddFile(ListOfFiles.SelectedItem.Text, "files");
    zip.AddStringAsFile(ReadmeText, "Readme.txt", "");
    zip.Save();
  }

  Response.End();
}

</script>



<html>
<head>
    <link rel="stylesheet" href="style/basic.css">
</head>

<body>

    <form id="Form" runat="server">

        <div class="SampleHeader" style="width:<%=width%>">
            <h3> <span id="Title" runat="server" /> </h3>
            <span class="SampleTitle"><b>Select a file to zip:</b></span>
            <br/>
	    <asp:DropDownList class="Select" id="ListOfFiles" AutoPostBack runat="server"/>
            <br/>
	    <asp:Button id="btnGo" Text="Zip it!" AutoPostBack OnClick="btnGo_Click" runat="server"/>
        </div>

        <span style="color:red" id="ErrorMessage" runat="server"/>

    </form>

</body>
</html>

