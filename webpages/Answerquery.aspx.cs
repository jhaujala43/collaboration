﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace WebApplication1
{
    public partial class WebForm3 : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            //fetch existing answers for the query
            SqlConnection con = new SqlConnection("server=QUIDDITCH;database=forum;integrated security=true;");
            con.Open();
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "select name,message,date from post where postid="+Request.QueryString["p"];
            SqlDataReader dr = cmd.ExecuteReader();
            while(dr.Read())
            {
                Label2.Text = dr["name"].ToString();
                Label4.Text = dr["message"].ToString();
                Label6.Text = "(Posted on "+ dr["date"].ToString()+")";
            }
            dr.Close();
            SqlDataAdapter sqlDa = new SqlDataAdapter("select message,name,date,upvotes,downvotes from answers where postid="+ Request.QueryString["p"], con);
            DataTable dtb = new DataTable();
            sqlDa.Fill(dtb);
            if (dtb.Rows.Count > 0)
            {
                GridView1.DataSource = dtb;
                GridView1.DataBind();
                Label8.Visible = false;
            }
            else
            {

                div1.Visible = false;
                Label8.Visible = true;
            }
            con.Close();
            con.Open();
            SqlCommand cmd1 = new SqlCommand();
            cmd1.Connection = con;
            cmd1.CommandType = CommandType.Text;
            cmd1.CommandText = "select name from users where userid=" + Request.QueryString["u"];
            SqlDataReader dr1 = cmd1.ExecuteReader();
            dr1.Read();
            Label9.Text = dr1["name"].ToString();
            dr1.Close();
            con.Close();
        }

        protected void Button1_Click(object sender, EventArgs e)
        {
            //store new answer in database
            String url;
            SqlConnection con = new SqlConnection("server=QUIDDITCH;database=forum;integrated security=true;");
            con.Open();
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "insert into answers values(@postid, @userid, @name, @message, @date, 0, 0)";
            cmd.Parameters.AddWithValue("@postid", Request.QueryString["p"]);
            cmd.Parameters.AddWithValue("@userid", Request.QueryString["u"]);
            cmd.Parameters.AddWithValue("@name", Label9.Text);
            cmd.Parameters.AddWithValue("@message", txtHTMLContext.Text);
            cmd.Parameters.AddWithValue("@date", DateTime.UtcNow);
            cmd.ExecuteNonQuery();
            con.Close();
            //notify the user who posted the query that a new answer is available
            con.Open();
            SqlCommand cmd3 = new SqlCommand();
            cmd3.Connection = con;
            cmd3.CommandType = CommandType.Text;
            cmd3.CommandText = "select email from users where userid =(select userid from post where postid=" +Request.QueryString["p"] + ")";
            SqlDataReader dr1 = cmd3.ExecuteReader();
            while (dr1.Read())
            {
                String to = dr1["email"].ToString();
                String from = "msbeeforcss@outlook.com";
                MailMessage message = new MailMessage(from, to);

                String mailbody = @" Greetings from Microsoft BEE!";
                mailbody += "\n"+@" This mail is to notify you that a new answer is added to the query you posted on Microsoft BEE.";
                mailbody +="\n"+@" Your query was:";
                mailbody += Label4.Text;
                mailbody += @"
                            Login to Microsoft BEE to view the answer to your query.";
                message.Subject = "Answer Notification";
                message.Body = mailbody;
                message.BodyEncoding = Encoding.UTF8;
                message.IsBodyHtml = true;
                SmtpClient client = new SmtpClient("smtp.live.com", 587);
                System.Net.NetworkCredential basicCredential1 = new System.Net.NetworkCredential("msbeeforcss@outlook.com", "shutterisland123");
                client.EnableSsl = true;
                client.UseDefaultCredentials = false;
                client.Credentials = basicCredential1;
                try
                {
                    client.Send(message);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            dr1.Close();
            con.Close();
            url = "Viewposts.aspx?u="+ Request.QueryString["u"];
            Response.Write("<script language='javascript'>alert('Answer added!');window.location.href='"+url+"';</script>");
        }

        protected void Row_Selected(object sender, EventArgs e)
        {
            //code to upvote/downvote an answer
            String url;
            int id;
            url = "Viewposts.aspx?u=" + Request.QueryString["u"];
            ImageButton img = (sender as ImageButton);
            String commandname = img.CommandName;
            GridViewRow row = (img.NamingContainer as GridViewRow);
            int rowIndex = row.RowIndex;
            SqlConnection con = new SqlConnection("server=QUIDDITCH;database=forum;integrated security=true;");
            con.Open();
            SqlCommand cmd1 = new SqlCommand();
            cmd1.Connection = con;
            cmd1.CommandType = CommandType.Text;
            cmd1.CommandText = "select ansid from answers where name=@name and message=@message";
            cmd1.Parameters.AddWithValue("@name", row.Cells[1].Text);
            cmd1.Parameters.AddWithValue("@message", row.Cells[0].Text);
            SqlDataReader dr1 = cmd1.ExecuteReader();
            dr1.Read();
            id = Convert.ToInt32(dr1["ansid"]);
            con.Close();
            if (commandname == "like")
            {
                int c = 0;
                int f = 0;
                SqlConnection con1 = new SqlConnection("server=QUIDDITCH;database=forum;integrated security=true;");
                con1.Open();
                SqlCommand cmd3 = new SqlCommand();
                cmd3.Connection = con1;
                cmd3.CommandType = CommandType.Text;
                cmd3.CommandText ="select userid from known where ansid=@ansid and action=@action";
                cmd3.Parameters.AddWithValue("@ansid", id);
                cmd3.Parameters.AddWithValue("@action", "upvoted");
                SqlDataReader dr = cmd3.ExecuteReader();
                while(dr.Read())
                {
                    if(dr["userid"].ToString()==Request.QueryString["u"])
                    {
                        c = 1;
                    }
                }
                dr.Close();
                con1.Close();
                if(c==1)
                {
                    Response.Write("<script language='javascript'>alert('You have already upvoted this answer!');window.location.href='" + url + "';</script>");
                }
                if (c==0)
                {
                    con1.Open();
                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = con1;
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "update answers set upvotes=upvotes+1 where name=@name and message=@message";
                    cmd.Parameters.AddWithValue("@name", row.Cells[1].Text);
                    cmd.Parameters.AddWithValue("@message", row.Cells[0].Text);
                    cmd.ExecuteNonQuery();
                    con1.Close();
                    con1.Open();
                    SqlCommand cmd2 = new SqlCommand();
                    cmd2.Connection = con1;
                    cmd2.CommandType = CommandType.Text;
                    cmd2.CommandText = "insert into known values(@ansid, @userid, @action)";
                    cmd2.Parameters.AddWithValue("@ansid", id);
                    cmd2.Parameters.AddWithValue("@userid", Request.QueryString["u"]);
                    cmd2.Parameters.AddWithValue("@action", "upvoted");
                    cmd2.ExecuteNonQuery();
                    con1.Close();
                    con1.Open();
                    SqlCommand cmd4 = new SqlCommand();
                    cmd4.Connection = con1;
                    cmd4.CommandType = CommandType.Text;
                    cmd4.CommandText = "select action from known where ansid=@ansid and userid=@userid";
                    cmd4.Parameters.AddWithValue("@ansid", id);
                    cmd4.Parameters.AddWithValue("@userid", Request.QueryString["u"]);
                    SqlDataReader dr2 = cmd4.ExecuteReader();
                    while(dr2.Read())
                    {
                        if (dr2["action"].ToString() == "downvoted")
                        {
                            f = 1;
                        }
                    }
                    dr2.Close();
                    con1.Close();
                    if(f==1)
                    {
                        con1.Open();
                        SqlCommand cmd5 = new SqlCommand();
                        cmd5.Connection = con1;
                        cmd5.CommandType = CommandType.Text;
                        cmd5.CommandText = "delete from known where ansid=@ansid and userid=@userid and action=@action";
                        cmd5.Parameters.AddWithValue("@ansid", id);
                        cmd5.Parameters.AddWithValue("@userid", Request.QueryString["u"]);
                        cmd5.Parameters.AddWithValue("@action", "downvoted");
                        cmd5.ExecuteNonQuery();
                        con1.Close();
                        con1.Open();
                        SqlCommand cmd6 = new SqlCommand();
                        cmd6.Connection = con1;
                        cmd6.CommandType = CommandType.Text;
                        cmd6.CommandText = "update answers set downvotes=downvotes-1 where name=@name and message=@message";
                        cmd6.Parameters.AddWithValue("@name", row.Cells[1].Text);
                        cmd6.Parameters.AddWithValue("@message", row.Cells[0].Text);
                        cmd6.ExecuteNonQuery();
                        con1.Close();
                    }
                    Response.Write("<script language='javascript'>alert('You upvoted this answer!');window.location.href='" + url + "';</script>");
                }
            }
            else if (commandname == "dislike")
            {
                int c = 0;
                int f = 0;
                SqlConnection con1 = new SqlConnection("server=QUIDDITCH;database=forum;integrated security=true;");
                con1.Open();
                SqlCommand cmd3 = new SqlCommand();
                cmd3.Connection = con1;
                cmd3.CommandType = CommandType.Text;
                cmd3.CommandText = "select userid from known where ansid=@ansid and action=@action";
                cmd3.Parameters.AddWithValue("@ansid", id);
                cmd3.Parameters.AddWithValue("@action", "downvoted");
                SqlDataReader dr = cmd3.ExecuteReader();
                while (dr.Read())
                {
                    if (dr["userid"].ToString() == Request.QueryString["u"])
                    {
                        c = 1;
                    }
                }
                dr.Close();
                con1.Close();
                if(c==1)
                {
                    Response.Write("<script language='javascript'>alert('You have already downvoted this answer!');window.location.href='" + url + "';</script>");
                }
                if (c == 0)
                {
                    con1.Open();
                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = con1;
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "update answers set downvotes=downvotes+1 where name=@name and message=@message";
                    cmd.Parameters.AddWithValue("@name", row.Cells[1].Text);
                    cmd.Parameters.AddWithValue("@message", row.Cells[0].Text);
                    cmd.ExecuteNonQuery();
                    con1.Close();
                    con1.Open();
                    SqlCommand cmd2 = new SqlCommand();
                    cmd2.Connection = con1;
                    cmd2.CommandType = CommandType.Text;
                    cmd2.CommandText = "insert into known values(@ansid, @userid, @action)";
                    cmd2.Parameters.AddWithValue("@ansid", id);
                    cmd2.Parameters.AddWithValue("@userid", Request.QueryString["u"]);
                    cmd2.Parameters.AddWithValue("@action", "downvoted");
                    cmd2.ExecuteNonQuery();
                    con1.Close();
                    con1.Open();
                    SqlCommand cmd4 = new SqlCommand();
                    cmd4.Connection = con1;
                    cmd4.CommandType = CommandType.Text;
                    cmd4.CommandText = "select action from known where ansid=@ansid and userid=@userid";
                    cmd4.Parameters.AddWithValue("@ansid", id);
                    cmd4.Parameters.AddWithValue("@userid", Request.QueryString["u"]);
                    SqlDataReader dr2 = cmd4.ExecuteReader();
                    while (dr2.Read())
                    {
                        if (dr2["action"].ToString() == "upvoted")
                        {
                            f = 1;
                        }
                    }
                    dr2.Close();
                    con1.Close();
                    if (f == 1)
                    {
                        con1.Open();
                        SqlCommand cmd5 = new SqlCommand();
                        cmd5.Connection = con1;
                        cmd5.CommandType = CommandType.Text;
                        cmd5.CommandText = "delete from known where ansid=@ansid and userid=@userid and action=@action";
                        cmd5.Parameters.AddWithValue("@ansid", id);
                        cmd5.Parameters.AddWithValue("@userid", Request.QueryString["u"]);
                        cmd5.Parameters.AddWithValue("@action", "upvoted");
                        cmd5.ExecuteNonQuery();
                        con1.Close();
                        con1.Open();
                        SqlCommand cmd6 = new SqlCommand();
                        cmd6.Connection = con1;
                        cmd6.CommandType = CommandType.Text;
                        cmd6.CommandText = "update answers set upvotes=upvotes-1 where name=@name and message=@message";
                        cmd6.Parameters.AddWithValue("@name", row.Cells[1].Text);
                        cmd6.Parameters.AddWithValue("@message", row.Cells[0].Text);
                        cmd6.ExecuteNonQuery();
                        con1.Close();
                    }
                    Response.Write("<script language='javascript'>alert('You downvoted this answer!');window.location.href='" + url + "';</script>");
                }
            }
        }

        protected void Button2_Click(object sender, EventArgs e)
        {
            //Redirect to previous page(feed page)
            String url;
            url = "Viewposts.aspx?u="+ Request.QueryString["u"];
            Response.Redirect(url);
        }
    }
}