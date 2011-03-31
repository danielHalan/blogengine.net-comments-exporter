using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using BlogEngine.Core;
using BlogEngine.Core.Web.Controls;

namespace Halan.Extensions {
	public partial class CommentsExporterView : System.Web.UI.Page {

			
		protected void Page_Load(object sender, EventArgs e) {

			if( !IsPostBack ) {
				
				BindAuthors();
				BindPosts();
			}

			BindMap();
		}

		private void BindPosts() {
			foreach( Post p in Post.Posts ) {
				if( !p.IsDeleted && p.IsPublished && CommentsExporter.HasValidComments(p) ) {
					ListItem li = new ListItem(
													string.Format("{0} ({1})", CutString(p.Title, 57), p.DateCreated.ToShortDateString()),
													p.Id.ToString(), true);
					li.Selected = true;

					cblPosts.Items.Add(li);
				}
			}
			
			

		}
		private void BindMap() {
			if( AuthorMaps.Count > 0 ) {
				lbEmpty.Visible = false;

				repMap.Visible = true;
				repMap.DataSource = AuthorMaps;
				repMap.DataBind();

			} else {

				repMap.Visible = false;
				lbEmpty.Visible = true;
			}

		}
		private void BindAuthors() {

			List<Comment> allComments = new List<Comment>();
			foreach( Post p in Post.Posts )
				CollectAllComments(p.Comments, allComments);

			List<AuthorData> authors = new List<AuthorData>();
			foreach( Comment c in allComments ) {
				if( (c.Email != "pingback") && (c.Email != "trackback") && !Contains(authors, c) )
					authors.Add(new AuthorData(c.Author, c.Email, c.Website));
			}

			authors.Sort(AuthorComparer);

			foreach( AuthorData a in authors )
				ddlCurrentName.Items.Add(new ListItem(string.Format("{0} ({1})", a.Name, a.Email), a.ToString()));


			if( ddlCurrentName.Items.Count == 0 )
				btnAddNameMap.Enabled = false;
			else {
				lbAuthorEmail.Text = !string.IsNullOrEmpty(authors[0].Email) ? authors[0].Email : "<i>No e-mail provided</i>";
				lbAuthorUrl.Text = !string.IsNullOrEmpty(authors[0].URL) ? authors[0].URL : "<i>No web page provided</i>";
			}
		}

		private string CutString(string str, int length) {
			return (str != null && str.Length > length) ? str.Substring(0, length-3) + "..." : str;
		}
		private int AuthorComparer(AuthorData a, AuthorData b) {
			return string.Compare(a.Name, b.Name);
		}
		private bool Contains(List<AuthorData> authors, Comment c) {
			foreach( AuthorData a in authors ) {
				if( a.Name.Equals(c.Author, StringComparison.CurrentCultureIgnoreCase) &&
						a.Email.Equals(c.Email, StringComparison.CurrentCultureIgnoreCase) )
					return true;
			}

			return false;
		}
		private void CollectAllComments(List<Comment> comments, List<Comment> bag) {
			foreach( Comment c in comments ) {
				if( c.IsApproved ) {
					bag.Add(c);

					CollectAllComments(c.Comments, bag);
				}
			}
		}
		
		protected void OnAddNameMapClick(object sender, EventArgs e) {
			if( ddlCurrentName.SelectedItem != null && tbDestName.Text.Length > 0 ) {

				AuthorData data = AuthorData.Parse(ddlCurrentName.SelectedItem.Value);
				AuthorMap map = new AuthorMap();
				map.BlogEngineAuthor = data;
				map.DesitnationAuthorName = tbDestName.Text;
				map.DesitnationAuthorEmail = !string.IsNullOrEmpty(tbDestEmail.Text) ? tbDestEmail.Text : data.Email;
				map.DesitnationAuthorURL = !string.IsNullOrEmpty(tbDestUrl.Text) ? tbDestUrl.Text : data.URL;
				AuthorMaps.Add(map);

				tbDestName.Text = string.Empty;
				tbDestEmail.Text = string.Empty;
				tbDestUrl.Text = string.Empty;

				ddlCurrentName.Items.RemoveAt(ddlCurrentName.SelectedIndex);
				if( ddlCurrentName.Items.Count == 0 )
					btnAddNameMap.Enabled = false;

				BindMap();

			}
		}
		protected void OnExportCommentClick(object sender, EventArgs e) {
			List<Guid> s = new List<Guid>();
			foreach( ListItem li in cblPosts.Items )
				if( li.Selected )
					s.Add(new Guid(li.Value));

			if( s.Count == 0 ) 
				return;
			
			Response.Clear();
			Response.AddHeader("Content-Disposition", "attachment; filename=Comments-WordPress.wxr");
			new CommentsExporter().Export(Response.OutputStream, s, AuthorMaps);
			Response.End();
		}

		private List<AuthorMap> AuthorMaps {
			get {
				if( ViewState["authMap"] == null )
					ViewState["authMap"] = new List<AuthorMap>();

				return (List<AuthorMap>)ViewState["authMap"];
			}
		}
	}
}