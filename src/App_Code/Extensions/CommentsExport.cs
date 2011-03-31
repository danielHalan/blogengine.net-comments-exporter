using System;
using System.Collections.Generic;
using System.Web;
using System.Xml;
using BlogEngine.Core;
using BlogEngine.Core.Web.Controls;
//using System.Xml.Linq;
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
		public string DesitnationAuthorName;
		public string DesitnationAuthorEmail;
		public string DesitnationAuthorURL;
	}

	public class CommentsExporter  {

		private readonly string DATE_FORMAT = "yyyy-MM-dd HH:mm";

		private int commentCount = 0;

		List<AuthorMap> _authorsMap;

		const string nsCONTENT	= "http://purl.org/rss/1.0/modules/content/";
		const string nsDSQ			= "http://www.disqus.com/";
		const string nsWP				= "http://wordpress.org/export/1.0/";
		const string nsEXCERPT	= "http://wordpress.org/export/1.0/excerpt/";
		const string nsWFW			= "http://wellformedweb.org/CommentAPI/";
		const string nsDC				=	"http://purl.org/dc/elements/1.1/";

		Dictionary<string, string> _ns = new Dictionary<string, string>(3); 

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
					return a.DesitnationAuthorName;

			return name;
		}
		string GetAuthorEmail(string name, string email) {
			foreach( AuthorMap a in _authorsMap )
				if( a.BlogEngineAuthor.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase) &&
					( a.BlogEngineAuthor.Email.Equals(email, StringComparison.CurrentCultureIgnoreCase) ) )
					return a.DesitnationAuthorEmail;

			return (email != "pingback" && email != "trackback") ? email : string.Empty;
		}
		string GetAuthorWebPage(string name, string email, Uri uri) {
			string url = uri != null ? uri.ToString() : string.Empty;
			
			foreach( AuthorMap a in _authorsMap )
				if( a.BlogEngineAuthor.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase) &&
					( a.BlogEngineAuthor.Email.Equals(email, StringComparison.CurrentCultureIgnoreCase) ) )
					return !string.IsNullOrEmpty(a.DesitnationAuthorURL) ? a.DesitnationAuthorURL : url;

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
			cmt.AppendChild(XElement("wp:comment_id", (++commentCount).ToString() ) );
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
				AddComment(root, sub, commentCount);
			}
		}


	}









	/*
	public class DisqusCommentExporter : ICommentExporter {
		public void Export(Stream output, Dictionary<string, string> authorsMap) {
			XmlDocument xDoc = new XmlDocument();
			// XML declaration
			XmlNode declaration = xDoc.CreateNode(XmlNodeType.XmlDeclaration, null, null);
			xDoc.AppendChild(declaration);

			// Root element: output
			XmlElement root = xDoc.CreateElement("articles");
			xDoc.AppendChild(root);

			foreach( Post post in Post.Posts ) {
				if( post.Comments.Count == 0 )
					continue;

				// Sub-element: article
				XmlElement article = xDoc.CreateElement("article");
				root.AppendChild(article);

				#region Article

				// Sub-element: url
				XmlElement url = xDoc.CreateElement("url");
				url.InnerText = post.AbsoluteLink.ToString();
				article.AppendChild(url);

				#region Comments

				// Sub-element: comments
				XmlElement comments = xDoc.CreateElement("comments");
				article.AppendChild(comments);

				foreach( Comment comment in post.Comments ) {

					if( comment.Email == "pingback" || comment.Email == "trackback" )
						continue;

					XmlElement commentRoot = xDoc.CreateElement("comment");
					comments.AppendChild(commentRoot);

					bool isAnnonymous = true;

					if( authorsMap.ContainsKey(comment.Author) )
						isAnnonymous = false;

					XmlElement name = xDoc.CreateElement("name");
					name.InnerText = isAnnonymous ? comment.Author : authorsMap[comment.Author];
					commentRoot.AppendChild(name);

					XmlElement email = xDoc.CreateElement("email");
					email.InnerText = comment.Email;
					commentRoot.AppendChild(email);

					// Sub-element: url
					XmlElement website = xDoc.CreateElement("url");
					if( comment.Website != null )
						website.InnerText = comment.Website.ToString();
					commentRoot.AppendChild(website);

					XmlElement ip = xDoc.CreateElement("ip_address");
					ip.InnerText = comment.IP;
					commentRoot.AppendChild(ip);

					XmlElement message = xDoc.CreateElement("message");
					message.InnerText = comment.Content;
					commentRoot.AppendChild(message);

					// todo: fix date and gmt
					XmlElement date = xDoc.CreateElement("date");
					date.InnerText = comment.DateCreated.ToString("ddd, dd MMM yyyy HH:mm:ss -0000");
					commentRoot.AppendChild(date);

					XmlElement points = xDoc.CreateElement("points");
					points.InnerText = "1";
					commentRoot.AppendChild(points);

				}

				#endregion

				#endregion

			}

			xDoc.Save(output);
		}
	}
	*/

	/*
	public class IntenseDebateCommentExporter : ICommentExporter {
		public void Export(Stream output, Dictionary<string, string> authorsMap) {
			XmlDocument xDoc = new XmlDocument();
			// XML declaration
			XmlNode declaration = xDoc.CreateNode(XmlNodeType.XmlDeclaration, null, null);
			xDoc.AppendChild(declaration);

			// Root element: output
			XmlElement root = xDoc.CreateElement("output");
			xDoc.AppendChild(root);

			foreach( Post post in Post.Posts ) {
				if( post.Comments.Count == 0 )
					continue;

				// Sub-element: blogpost
				XmlElement blogpost = xDoc.CreateElement("blogpost");
				root.AppendChild(blogpost);

				// Sub-element: url
				XmlElement url = xDoc.CreateElement("url");
				url.InnerText = HttpUtility.UrlEncode(post.AbsoluteLink.ToString());
				blogpost.AppendChild(url);

				// Sub-element: title
				XmlElement title = xDoc.CreateElement("title");
				title.InnerText = post.Title;
				blogpost.AppendChild(title);

				// Sub-element: guid
				XmlElement guid = xDoc.CreateElement("guid");
				guid.InnerText = post.PermaLink.ToString();
				blogpost.AppendChild(guid);

				#region Comments

				// comments
				XmlElement comments = xDoc.CreateElement("comments");
				blogpost.AppendChild(comments);

				foreach( Comment comment in post.Comments ) {

					XmlElement commentRoot = xDoc.CreateElement("comment");
					comments.AppendChild(commentRoot);

					bool isAnnonymous = true;

					if( authorsMap.ContainsKey(comment.Author) )
						isAnnonymous = false;

					// Sub-element: isAnon
					XmlElement isAnon = xDoc.CreateElement("isAnon");
					isAnon.InnerText = isAnnonymous ? "1" : "0";
					commentRoot.AppendChild(isAnon);

					// Sub-element: name
					XmlElement name = xDoc.CreateElement("name");
					name.InnerText = isAnnonymous ? comment.Author : authorsMap[comment.Author];
					commentRoot.AppendChild(name);

					// Sub-element: email
					XmlElement email = xDoc.CreateElement("email");
					email.InnerText = comment.Email;
					commentRoot.AppendChild(email);

					// Sub-element: url
					XmlElement website = xDoc.CreateElement("url");
					if( comment.Website != null )
						website.InnerText = comment.Website.ToString();
					commentRoot.AppendChild(website);

					// Sub-element: ip
					XmlElement ip = xDoc.CreateElement("ip");
					ip.InnerText = IPAddressToNumber(comment.IP).ToString();
					commentRoot.AppendChild(ip);

					// Sub-element: comment
					XmlElement commentContent = xDoc.CreateElement("comment");
					commentContent.InnerText = comment.Content;
					commentRoot.AppendChild(commentContent);

					// todo: fix date and gmt
					// Sub-element: date
					XmlElement date = xDoc.CreateElement("date");
					date.InnerText = comment.DateCreated.ToString("yyyy-MM-dd HH:mm:ss");
					commentRoot.AppendChild(date);

					// Sub-element: gmt
					XmlElement gmt = xDoc.CreateElement("gmt");
					gmt.InnerText = comment.DateCreated.ToString("yyyy-MM-dd HH:mm:ss");
					commentRoot.AppendChild(gmt);

					// Sub-element: score
					XmlElement score = xDoc.CreateElement("score");
					score.InnerText = isAnnonymous ? "0" : "1";
					commentRoot.AppendChild(score);

				}

				#endregion

			}

			xDoc.Save(output);
		}


		public static double IPAddressToNumber(string IPaddress) {
			int i;
			string[] arrDec;
			double num = 0;
			if( IPaddress == "" ) {
				return 0;
			} else {
				arrDec = IPaddress.Split('.');
				for( i = arrDec.Length - 1;i >= 0;i-- ) {
					num += ( ( int.Parse(arrDec[i]) % 256 ) * Math.Pow(256, ( 3 - i )) );
				}
				return num;
			}
		}
	}
	*/
}