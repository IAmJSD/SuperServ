using Nancy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SuperServ
{
    public class WebsiteModule : NancyModule
    {
        public ValueTuple<string, User> CheckAuthCookie(NancyContext context)
        {
            if (!context.Request.Cookies.ContainsKey("auth"))
            {
                return (null, null);
            }
            var cookie = context.Request.Cookies["auth"];
            if (cookie == null)
            {
                return (null, null);
            }
            AccessTokenInfo auth_info;
            try
            {
                auth_info = Program.config_handler.config.access_tokens[cookie];
            }
            catch (Exception)
            {
                return (null, null);
            }
            if ((int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds > auth_info.active_until)
            {
                return (null, null);
            }
            try
            {
                var user = Program.config_handler.config.users[auth_info.user_id];
                if (user.disabled)
                {
                    return (null, null);
                }
                return (auth_info.user_id, user);
            } catch(Exception)
            {
                return (null, null);
            }
        }
        // Checks the authentication cookie, returns null if it is invalid and a user if not.

        public Response DeauthAndRedirect(string path, NancyContext context)
        {
            if (context.Request.Cookies.ContainsKey("auth"))
            {
                try
                {
                    Program.config_handler.config.access_tokens.Remove(context.Request.Cookies["auth"]);
                } catch(Exception) { }
            }
            var response = new Nancy.Responses.RedirectResponse(path);
            response.Cookies.Add(new Nancy.Cookies.NancyCookie("auth", "", DateTime.UtcNow));
            return response;
        }
        // Deauthorises the user and redirects.

        public WebsiteModule()
        {
            Get("/", _ => {
                if (Context.Request.Cookies.ContainsKey("auth"))
                {
                    return new Nancy.Responses.RedirectResponse("/a");
                }
                var template = TemplateCacher.ReadTemplate("./templates/index.html");
                return new Nancy.Response
                {
                    StatusCode = Nancy.HttpStatusCode.OK,
                    ContentType = "text/html",
                    Contents = stream => (new StreamWriter(stream) { AutoFlush = true }).Write(template.Render(new
                    {
                        Name = XSSPrevention.XSSParse(Program.config_handler.config.server_name)
                    }))
                };
            });
            // Renders the login page (or redirects if there is a session cookie).

            Get("/a/{path_info?}", async args => {
                var user_tuple = CheckAuthCookie(Context);
                if (user_tuple.Item1 == null)
                {
                    return await DeauthAndRedirect("../", Context);
                }
                string uuid = user_tuple.Item1;
                User user = user_tuple.Item2;

                bool root = args.path_info == null;

                return "hi";
            });
            // The authenticated route for showing files/folders.

            Get("/f/{path*}", async args =>
            {
                var user_tuple = CheckAuthCookie(Context);
                if (user_tuple.Item1 == null)
                {
                    return await DeauthAndRedirect("../", Context);
                }
                string uuid = user_tuple.Item1;
                User user = user_tuple.Item2;

                string data = args.path;

                var file_info = Utils.GetFile(uuid, user, data);
                if (file_info == null)
                {
                    return "Either the file was not found or you do not have permission to read it.";
                }

                var file = new FileStream(file_info.path, FileMode.Open);

                var response = new Nancy.Responses.StreamResponse(() => file, MimeTypes.GetMimeType(file_info.name));
                return response.AsAttachment(file_info.name);
            });
            // Handles file downloading.
        }
    }
}
