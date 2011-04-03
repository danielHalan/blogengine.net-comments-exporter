<%@ Page Title="" Language="C#" MasterPageFile="~/admin/admin1.master" AutoEventWireup="true"
  CodeFile="CommentsExporterView.aspx.cs" Inherits="Halan.Extensions.CommentsExporterView" %>

<asp:Content ID="Content1" ContentPlaceHolderID="cphAdmin" runat="Server">

<script type="text/javascript">
  function strCut(str, len) {
    if (str.length > len) {
      return str.substr(0, len - 3) + "...";
    } else return str;
  }
  
  function onAuthorSelect(e) {
    var parts = e.value.split(",");
    document.getElementById("ctl00_cphAdmin_lbAuthorEmail").innerText = parts[1] != null && parts[1].length > 0 ? parts[1] : "<i>No e-mail provided</i>";
    document.getElementById("ctl00_cphAdmin_lbAuthorUrl").innerHTML = parts[2] != null && parts[2].length > 0 ? strCut(parts[2], 30) : "<i>No web page provided</i>";
    document.getElementById("aAuthurUrl").href = parts[2];
  }

  function togglePosts() {
    var i = 0;
    var ctl;
    var status = new Array(0, 0);

    while (ctl = document.getElementById("ctl00_cphAdmin_cblPosts_" + i++)) 
      status[+ctl.checked]++;

    var newState = status[0] > status[1];
    i = 0;
    while (ctl = document.getElementById("ctl00_cphAdmin_cblPosts_" + i++)) {
      ctl.checked = newState;
    };

  }

</script>

  <div class="settings">
    <h1>Comments Exporter</h1>

    <div style="padding-left: 40px; min-height: 200px;">
      <span style="font-size: 10pt; font-weight: bold;">Map Current Authors to New Author Names</span>
      <table cellspacing="4" style="border-bottom: 1px solid #C7C7C7;">
        <tr>
          <td><asp:DropDownList ID="ddlCurrentName" runat="server" onchange="onAuthorSelect(this);" style="min-width: 120px; max-width: 200px;" /></td>
          <td>=></td>
          <td colspan="2"><asp:TextBox ID="tbDestName" runat="server" /> <span style="color: Red;">*</span> </td>
        </tr>
        <tr>
          <td>
            <asp:Label ID="lbAuthorEmail" runat="server" />
          </td>
          <td>=></td>
          <td colspan="2"><asp:TextBox ID="tbDestEmail" runat="server" /></td>
        </tr>
        <tr>
          <td>
            <a id="aAuthurUrl" target="_new"><asp:Label ID="lbAuthorUrl" runat="server" /></a>
          </td>
          <td>=></td>
          <td><asp:TextBox ID="tbDestUrl" runat="server" /></td>
          <td><asp:Button ID="btnAddNameMap" runat="server" Text="Add Mapping" OnClick="OnAddNameMapClick" /></td>
        </tr>
      </table>
      
      <br />
    
       <span style="font-size: 10pt; font-weight: bold;">Author Name Mappings:</span>
       <br />

      <asp:Repeater ID="repMap" runat="server">
        <HeaderTemplate>
          <table border="0" cellspacing="0" style="min-width: 420px;  margin-top: 4px; border-top: 1px solid black; border-left: 1px solid black; border-right: 1px solid black;">
            <tr style="background-color: #CAD5DE; text-align:left;"> 
              <th style="padding: 6px; min-width: 120px; background-color: #C5D0D9">Current Name</th>
              <th style="padding: 6px; min-width: 140px;">Desination Name</th>
              <th style="padding: 6px; min-width: 140px; padding-right: 8px;">Desination Email</th>
            </tr>
        </HeaderTemplate>
        <ItemTemplate>  
          <tr>
            <td style="padding: 6px; border-bottom: 1px dotted #8C8C8C; background-color: #F0F0F0;">
              <%# ((Halan.Extensions.AuthorMap)Container.DataItem).BlogEngineAuthor.Name %>
            </td>
            <td style="padding: 6px; border-bottom: 1px dotted #8C8C8C; padding-left: 4px;">
              <%# ((Halan.Extensions.AuthorMap)Container.DataItem).DesitnationAuthor.Name%>
            </td>
            <td style="padding: 6px; border-bottom: 1px dotted #8C8C8C;">
              <%# ((Halan.Extensions.AuthorMap)Container.DataItem).DesitnationAuthor.Email%>
            </td>
          </tr>
        </ItemTemplate>
        <FooterTemplate>
          </table>
        </FooterTemplate>
      </asp:Repeater>
      <asp:Label ID="lbEmpty" runat="server" Text="No author name mappings defined" style="font-style:italic;" />

      <div style="padding-top: 24px;">

        <span style="font-size: 10pt; font-weight: bold;">Posts to Export:</span> <a href="#" onclick="togglePosts()">Toggle selection</a><br />
        <span>Available published posts that contains comments</span>
        <div style="max-height: 300px; overflow: auto; overflow-x: hidden; width: 510px; padding-top: 6px; ">
          <asp:CheckBoxList ID="cblPosts" runat="server" RepeatLayout="Flow" Width="500" style="border: 1px dashed gray; padding-left: 6px;" >
          </asp:CheckBoxList>
        </div>
      </div>    

      <br />
      <br />
    </div>
    
    <div style="float: none">
    </div>


    <div style="width: 540px; height: 200px; border-top: 1px solid #C7C7C7; padding-top: 6px;">
      <div style="width: 350px; float: left; color: #737373">
        The exported file is an WordPress WXR file, that can be easily imported to services such as DISQUS...
      </div>

      <div style="float:right;">
        <asp:Button ID="btnExportComment" Text="Export Comments" runat="server" OnClick="OnExportCommentClick" /> 
      </div>
    
    </div>



    <hr />
    BlogEngine.NET Comments Exporter v1.0, &copy;2011 Daniel Halan (<a href="http://www.halan.se">www.halan.se</a>)
  </div>
</asp:Content>
