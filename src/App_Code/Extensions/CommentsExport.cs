using System;
using System.Collections.Generic;
using System.Web;
using System.Xml;
using BlogEngine.Core;
using BlogEngine.Core.Web.Controls;
using System.IO;
using System.Collections.Specialized;
using System.Collections;

namespace Halan.Extensions {
  
  [Extension("Export comments, for use in another comment engines", "1.0", "<a href='http://www.halan.se' target='_blank'>Daniel Halan</a>")]
  public class CommentsExport {
    private const string EXTENSION_NAME = "CommentsExport";
    private const string EXTENSION_ADMIN_PAGE = "~/User controls/CommentsExporterView.aspx";

    public CommentsExport() {
      ExtensionManager.SetAdminPage(EXTENSION_NAME, EXTENSION_ADMIN_PAGE);
    }
  }
  
  [Serializable]
  public struct AuthorData {
    public string Name;
    public string Email;
    public string URL;

    public AuthorData(string name, string email, string url) {
      Name = name;
      Email = email;
      URL = url;
    }
    public AuthorData(string name, string email, Uri url) {
      Name = name;
      Email = email;
      URL = url != null ? url.ToString() : string.Empty;
    }

    public static AuthorData Parse(string data) {
      string[] parts = data.Split(',');
      return new AuthorData(parts[0], parts[1], parts[2]);
    }

    public override string ToString() {
      return string.Format("{0},{1},{2}", Name, Email, URL);
    }

  }

  [Serializable]
  public class AuthorMap {
    public AuthorData BlogEngineAuthor;
    public AuthorData DesitnationAuthor = new AuthorData();
  }

  public class CommentsExporter  {

    const string DATE_FORMAT = "yyyy-MM-dd HH:mm:ss";

    const string nsCONTENT  = "http://purl.org/rss/1.0/modules/content/";
    const string nsDSQ      = "http://www.disqus.com/";
    const string nsWP       = "http://wordpress.org/export/1.0/";
    const string nsEXCERPT  = "http://wordpress.org/export/1.0/excerpt/";
    const string nsWFW      = "http://wellformedweb.org/CommentAPI/";
    const string nsDC       =	"http://purl.org/dc/elements/1.1/";
    
    Dictionary<string, string> _ns = new Dictionary<string, string>(6); 

    int _commentCount = 0;

    List<AuthorMap> _authorsMap;

    XmlDocument _doc;

    public CommentsExporter() {
      _ns.Add("wp", nsWP);
      _ns.Add("dsq", nsDSQ);
      _ns.Add("content", nsCONTENT);
      _ns.Add("excerpt", nsEXCERPT);
      _ns.Add("wfw", nsWFW);
      _ns.Add("dc", nsDC);
    }

    public void Export(Stream output, List<Guid> selection, List<AuthorMap> authorsMap) {
      _authorsMap = authorsMap;
      _commentCount = 0;

      _doc = new XmlDocument();
      _doc.AppendChild( _doc.CreateNode(XmlNodeType.XmlDeclaration, null, null));


      // Root element: output 
      XmlElement rss = XElement("rss");
      foreach( KeyValuePair<string, string> itm in _ns ) 
        rss.SetAttribute("xmlns:" + itm.Key, itm.Value);				
      
      _doc.AppendChild(rss);

      XmlElement root = XElement("channel");
      rss.AppendChild(root);
      int commentId = 0;

      int postId = 0;
      foreach( Post post in Post.Posts ) {
        if( HasValidComments(post) && ( selection == null || Contains(selection, post.Id) ) ) {

          // Sub-element: article
          XmlElement item = XElement("item");
          root.AppendChild(item);

          item.AppendChild(XElement("title", post.Title));
          item.AppendChild(XElement("link", post.PermaLink.ToString()));
          item.AppendChild(XElement("pubDate", post.DateCreated.ToString(DATE_FORMAT)));
          item.AppendChild(XElement("dc:creator", GetAuthorName(post.Author, post.AuthorProfile != null ? post.AuthorProfile.EmailAddress : string.Empty)));
          item.AppendChild(XElement("description", string.Empty));
          item.AppendChild(XElement("content:encoded", _doc.CreateCDataSection(post.Content)));

          item.AppendChild(XElement("wp:post_id", ( ++postId ).ToString()));
          item.AppendChild(XElement("wp:post_date", post.DateCreated.ToString(DATE_FORMAT)));
          item.AppendChild(XElement("wp:post_date_gmt", post.DateCreated.ToString(DATE_FORMAT)));
          item.AppendChild(XElement("wp:comment_status", post.IsCommentsEnabled ? "open" : "closed"));
          item.AppendChild(XElement("wp:ping_status", post.IsVisibleToPublic ? "open" : "closed"));
          item.AppendChild(XElement("wp:post_type", "post"));

          foreach( Comment comment in post.NestedComments ) {
            AddComment(item, comment, null);
          }

        }
      }

      _doc.Save(output);
    }


    private XmlAttribute XAttribute(string name, string value) {
      XmlAttribute atr = _doc.CreateAttribute(name);
      atr.Value = value;

      return atr;
    }

    private XmlElement XElement(string name, string value, params XmlAttribute []attributes) {
      XmlElement e = null;

      if( name.IndexOf(':') != -1 ) 
        e = _doc.CreateElement(name, _ns[name.Split(':')[0]]);
      else e = _doc.CreateElement(name);
      
      if( value != null )
        e.InnerText = value;

      if( attributes  != null )
        foreach( XmlAttribute atr in attributes )
          e.Attributes.Append(atr);

      return e;
    }
    private XmlElement XElement(string name, params XmlAttribute[] attributes) {
      return XElement(name, null, attributes);
    }
    private XmlElement XElement(string name) {
      return XElement(name, null, (XmlAttribute[])null);
    }
    private XmlElement XElement(string name, params XmlLinkedNode[] childs) {
      XmlElement e = null;

      if( name.IndexOf(':') != -1 )
        e = _doc.CreateElement(name, _ns[name.Split(':')[0]]);
      else e = _doc.CreateElement(name);

      if( childs != null )
        foreach( XmlLinkedNode child in childs )
          e.AppendChild(child);

      return e;
    }

    string GetAuthorName(string name, string email) {
      foreach(AuthorMap a in _authorsMap )
        if( a.BlogEngineAuthor.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase) && 
          ( a.BlogEngineAuthor.Email.Equals(email, StringComparison.CurrentCultureIgnoreCase) ) )
          return a.DesitnationAuthor.Name;

      return name;
    }
    string GetAuthorEmail(string name, string email) {
      foreach( AuthorMap a in _authorsMap )
        if( a.BlogEngineAuthor.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase) &&
          ( a.BlogEngineAuthor.Email.Equals(email, StringComparison.CurrentCultureIgnoreCase) ) )
          return a.DesitnationAuthor.Email;

      return (email != "pingback" && email != "trackback") ? email : string.Empty;
    }
    string GetAuthorWebPage(string name, string email, Uri uri) {
      string url = uri != null ? uri.ToString() : string.Empty;
      
      foreach( AuthorMap a in _authorsMap )
        if( a.BlogEngineAuthor.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase) &&
          ( a.BlogEngineAuthor.Email.Equals(email, StringComparison.CurrentCultureIgnoreCase) ) )
          return !string.IsNullOrEmpty(a.DesitnationAuthor.URL) ? a.DesitnationAuthor.URL : url;

      return url;
    }

    

    private static bool IsValidComment(Comment c) {
      return ( c.IsApproved && c.Email != "pingback" && c.Email != "trackback" );
    }
    public static bool HasValidComments(Post p) {
      if( p.Comments.Count == 0 )
        return false;

      foreach( Comment c in p.Comments ) {
        if( IsValidComment(c) )
          return true;
      }

      return false;
    }
    bool Contains(List<Guid> selection, Guid id) {
      foreach( Guid itm in selection )
        if( itm.Equals(id) )
          return true;

      return false;
    }


    void AddComment(XmlElement root, Comment comment, int? parentId) {

      string commentType = string.Empty;

      if( comment.Email == "pingback" || comment.Email == "trackback" ) {
        commentType = "pingback";

        if( comment.Email == "pingback" )
          comment.Content = comment.Content.Replace(string.Format("Pingback from {0}", comment.Author), string.Empty);
        else comment.Content = comment.Content.Replace(string.Format(" - Trackback from {0}", comment.Author), string.Empty);
        comment.Content += string.Format(" ({0})", comment.Email); 


        comment.Email = string.Empty;
      }


      XmlElement cmt = XElement("wp:comment");
      cmt.AppendChild(XElement("wp:comment_id", (++_commentCount).ToString() ) );
      cmt.AppendChild(XElement("wp:comment_author", _doc.CreateCDataSection(GetAuthorName(comment.Author, comment.Email))));
      cmt.AppendChild(XElement("wp:comment_author_email", GetAuthorEmail(comment.Author, comment.Email)));
      cmt.AppendChild(XElement("wp:comment_author_url", GetAuthorWebPage(comment.Author, comment.Email, comment.Website)));
      cmt.AppendChild(XElement("wp:comment_author_IP", comment.IP));
      cmt.AppendChild(XElement("wp:comment_date", comment.DateCreated.ToString(DATE_FORMAT)));
      cmt.AppendChild(XElement("wp:comment_date_gmt", comment.DateCreated.ToString(DATE_FORMAT)));
      cmt.AppendChild(XElement("wp:comment_content", _doc.CreateCDataSection(comment.Content)));
      cmt.AppendChild(XElement("wp:comment_approved", comment.IsApproved ? "1" : "0"));
      cmt.AppendChild(XElement("wp:comment_type", commentType));
      
      cmt.AppendChild(XElement("wp:comment_parent", parentId != null ? parentId.ToString() : "0"));

      root.AppendChild(cmt);

      foreach( Comment sub in comment.Comments ) {
        AddComment(root, sub, _commentCount);
      }
    }

  }

}