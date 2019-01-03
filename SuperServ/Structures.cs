using System;
using System.Collections.Generic;
using System.Text;

namespace SuperServ
{
    public class PermInfo
    {
        public bool write { get; set; }
        public bool delete_inside { get; set; }
        public bool delete_folder { get; set; }
        public bool read { get; set; }
    }

    public class PathInfo: PermInfo
    {
        public string perm_added_by { get; set; }
    }

    public class Group
    {
        public Dictionary<int, string> group_user_modifications { get; set; }
        public string group_name { get; set; }
        public bool administrator { get; set; }
        public bool user_default { get; set; }
        public PermInfo default_perms { get; set; }
        public Dictionary<string, PathInfo> paths { get; set; }
    }

    public class User
    {
        public string email { get; set; }
        public bool email_verified { get; set; }
        public bool must_change_password { get; set; }
        public bool disabled { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public int last_login { get; set; }
        public List<string> groups { get; set; }
        public Dictionary<string, PathInfo> path_overrides { get; set; }
    }

    public class AccessTokenInfo
    {
        public int active_until { get; set; }
        public string user_id { get; set; }
    }

    public class MailGunOptions
    {
        public string api_key { get; set; }
        public string email { get; set; }
        public string domain { get; set; }
    }

    public class SendgridOptions
    {
        public string api_key { get; set; }
        public string name { get; set; }
        public string email { get; set; }
    }

    public class CredentialResetOptions
    {
        public bool enabled { get; set; }
        public MailGunOptions mailgun { get; set; }
        public SendgridOptions sendgrid { get; set; }
    }

    public class Config
    {
        public int port { get; set; }
        public bool password_complexity { get; set; }
        public bool hibp_password_check { get; set; }
        public string server_name { get; set; }
        public Dictionary<string, User> users { get; set; }
        public Dictionary<string, Group> user_groups { get; set; }
        public Dictionary<string, AccessTokenInfo> access_tokens { get; set; }
        public CredentialResetOptions credential_reset_options { get; set; }
    }

    public class AuthPOST
    {
        public string username { get; set; }
        public string password { get; set; }
    }

    public class UserEditPOST
    {
        public string user_uuid { get; set; }
        public string password { get; set; }
        public string username { get; set; }
        public string new_password { get; set; }
        public string email { get; set; }
    }

    public class GenericAPIResponse
    {
        public string type { get; set; }
        public bool success { get; set; }
        public string message { get; set; }
    }

    public class RequestedUserEdits
    {
        public GenericAPIResponse username { get; set; }
        public GenericAPIResponse new_password { get; set; }
        public GenericAPIResponse email { get; set; }
    }

    public class UserEditResponse
    {
        public RequestedUserEdits requested_edits { get; set; }
        public string type { get; set; }
        public bool success { get; set; }
    }

    public class UserPath: PermInfo
    {
        public string name { get; set; }
        public string real_path { get; set; }
        public List<UserPath> children { get; set; }
    }

    public class AuthCheckInternalResponse
    {
        public Nancy.Response err { get; set; }
        public bool authed { get; set; }
        public KeyValuePair<string, AccessTokenInfo> access_token_pair { get; set; }
    }

    public class BasicFileFolderInfo
    {
        public string name { get; set; }
        public string path { get; set; }
    }

    public class UserPathExt : UserPath
    {
        public List<BasicFileFolderInfo> files { get; set; }
        public List<BasicFileFolderInfo> folders { get; set; }
    }
}
