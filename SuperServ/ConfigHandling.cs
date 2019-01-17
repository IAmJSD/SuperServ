using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using Console = Colorful.Console;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace SuperServ
{
    class ConfigHandling
    {
        public Config config;

        public string ConfigLocation;

        public void CreateNewConfig()
        {
            int unix_timestamp = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            // Defines a UNIX timestamp.

            var c = new Config();
            c.port = 8080;
            c.user_groups = new Dictionary<string, Group>();
            c.users = new Dictionary<string, User>();
            c.access_tokens = new Dictionary<string, AccessTokenInfo>();
            c.password_complexity = true;
            c.hibp_password_check = true;
            c.server_name = "SuperServ";
            c.credential_reset_options = new CredentialResetOptions()
            {
                enabled = false,
                sendgrid = new SendgridOptions(),
                mailgun = new MailGunOptions(),
            };
            // Defines the config.

            string AllUsersGroupUUID = Guid.NewGuid().ToString();
            string AdministratorsGroupUUID = Guid.NewGuid().ToString();
            string AdministratorUUID = Guid.NewGuid().ToString();
            // Defines all of the new UUID's.

            var administrator = new User()
            {
                username = "admin",
                groups = new List<string>()
                {
                    AllUsersGroupUUID,
                    AdministratorsGroupUUID
                },
                last_login = 1,
                must_change_password = true,
                path_overrides = new Dictionary<string, PathInfo>(),
                password = BCrypt.Net.BCrypt.HashPassword("Password1!"),
                disabled = false,
                email_verified = false,
            };
            // Defines the new administrator user.

            var all_user = new Group()
            {
                group_name = "All Users",
                administrator = false,
                user_default = true,
                paths = new Dictionary<string, PathInfo>(),
                default_perms = new PermInfo()
                {
                    write = false,
                    delete_folder = false,
                    delete_inside = false,
                    read = true
                },
                group_user_modifications =  new Dictionary<int, string>(),
            };
            all_user.group_user_modifications[unix_timestamp] = AdministratorUUID;
            // Defines the all users group.

            var administrators = new Group()
            {
                group_name = "Administrators",
                administrator = true,
                user_default = false,
                paths = new Dictionary<string, PathInfo>(),
                default_perms = new PermInfo()
                {
                    write = true,
                    delete_folder = true,
                    delete_inside = true,
                    read = true
                },
                group_user_modifications = new Dictionary<int, string>(),
            };
            administrators.group_user_modifications[unix_timestamp] = AdministratorUUID;
            // Defines the administrators group.

            c.user_groups[AdministratorsGroupUUID] = administrators;
            c.user_groups[AllUsersGroupUUID] = all_user;
            // Sets the user groups.

            c.users[AdministratorUUID] = administrator;
            // Sets the user.

            config = c;
            // Sets the config.
        }
        // Creates a new default config.

        public void SaveConfig()
        {
            try
            {
                string data = JsonConvert.SerializeObject(config, Formatting.Indented);
                FileStream file = File.Open(ConfigLocation, FileMode.Create);
                file.Write(Encoding.UTF8.GetBytes(data));
                file.Close();
            } catch(Exception) {
                Console.WriteLine("ERROR: Failed to save config. Oh no, that's not good.", Color.Red);
            }
        }
        // Saves the config.

        public void LoadConfig()
        {
            ConfigLocation = Environment.GetEnvironmentVariable("CONFIG_LOCATION");
            if (ConfigLocation == null) {
                ConfigLocation = "config.json";
            }

            try
            {
                using (StreamReader r = new StreamReader(ConfigLocation))
                {
                    string json = r.ReadToEnd();
                    config = JsonConvert.DeserializeObject<Config>(json);
                }
            } catch(Exception) {
                Console.WriteLine("WARNING: No config found so a new one is being generated. When you login, use the username admin and the password Password1! - you will be prompted to change your password.", Color.Yellow);
                CreateNewConfig();
                SaveConfig();
            }
        }
        // Loads the config.

        public List<KeyValuePair<string, User>> GetUsers()
        {
            List<KeyValuePair<string, User>> user_list = new List<KeyValuePair<string, User>>();
            foreach (KeyValuePair<string, User> user_kv_pair in config.users)
            {
                user_list.Add(user_kv_pair);
            }
            return user_list;
        }
        // Gets all of the users.

        public List<UserPath> GetPathPerms(string uid, User user)
        {
            bool administrator = false;
            UniquehighestPermDict Paths = new UniquehighestPermDict();

            foreach (string group_string in user.groups)
            {
                Group group = config.user_groups[group_string];
                foreach (KeyValuePair<string, PathInfo> path in group.paths)
                {
                    Paths.Add(path.Key, path.Value);
                }
                if (group.administrator)
                {
                    administrator = true;
                }
            }

            if (administrator)
            {
                foreach (string key in Paths.Keys)
                {
                    Paths[key].delete_folder = true;
                    Paths[key].delete_inside = true;
                    Paths[key].read = true;
                    Paths[key].write = true;
                }
                var all = new PathInfo()
                {
                    write = true,
                    read = true,
                    delete_folder = true,
                    delete_inside = true
                };
                foreach (string group_key in config.user_groups.Keys.Where(p => !user.groups.Any(l => p == l)))
                {
                    var group = config.user_groups[group_key];
                    foreach (string path in group.paths.Keys)
                    {
                        Paths[path] = all;
                    }
                }
                foreach (KeyValuePair<string, User> user_pair in config.users)
                {
                    if (user_pair.Key == uid)
                    {
                        continue;
                    }
                    foreach (string path in user.path_overrides.Keys)
                    {
                        Paths[path] = all;
                    }
                }
            }

            foreach (KeyValuePair<string, PathInfo> path in user.path_overrides)
            {
                Paths.Add(path.Key, path.Value, true);
            }

            if (Paths.Count == 0)
            {
                return new List<UserPath>();
            }

            Dictionary<int, List<KeyValuePair<string, PathInfo>>> path_lengths = new Dictionary<int, List<KeyValuePair<string, PathInfo>>>();

            char SlashType;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                SlashType = '\\';
            }
            else
            {
                SlashType = '/';
            }

            foreach (KeyValuePair<string, PathInfo> path in Paths)
            {
                try
                {
                    path_lengths[path.Key.Split(SlashType).Length].Add(path);
                }
                catch (Exception)
                {
                    path_lengths[path.Key.Split(SlashType).Length] = new List<KeyValuePair<string, PathInfo>>() { path };
                }
            }

            var keys = path_lengths.Keys.ToList();
            keys.Sort();

            Dictionary<string, UserPath> RawPath_Result = new Dictionary<string, UserPath>();
            var smallest_key = keys[0];
            var smallest_length_paths = path_lengths[keys[0]];

            keys.RemoveAt(0);
            path_lengths.Remove(smallest_key);

            foreach (KeyValuePair<string, PathInfo> pair in smallest_length_paths)
            {
                var key_split = pair.Key.Split(SlashType);
                RawPath_Result[pair.Key] = new UserPath()
                {
                    children = new List<UserPath>(),
                    delete_folder = pair.Value.delete_folder,
                    delete_inside = pair.Value.delete_inside,
                    name = key_split[key_split.Length - 1],
                    read = pair.Value.read,
                    real_path = pair.Key,
                    write = pair.Value.write
                };
            }

            foreach (int list_key in keys)
            {
                List<KeyValuePair<string, PathInfo>> list = path_lengths[list_key];
                foreach (KeyValuePair<string, PathInfo> pair in list)
                {
                    bool cont = true;
                    var key_split = pair.Key.Split(SlashType);
                    var UsrPath = new UserPath()
                    {
                        children = new List<UserPath>(),
                        delete_folder = pair.Value.delete_folder,
                        delete_inside = pair.Value.delete_inside,
                        name = key_split[key_split.Length - 1],
                        read = pair.Value.read,
                        real_path = pair.Key,
                        write = pair.Value.write
                    };
                    foreach (var parent in RawPath_Result.Keys)
                    {
                        if (pair.Key.ToLower().StartsWith(parent.ToLower()))
                        {
                            UserPath pathinfo_ptr = RawPath_Result[parent];
                            while (true)
                            {
                                bool further = false;
                                foreach (UserPath child in pathinfo_ptr.children)
                                {
                                    if (pair.Key.ToLower().StartsWith(child.real_path.ToLower()))
                                    {
                                        further = true;
                                        pathinfo_ptr = child;
                                        break;
                                    }
                                }
                                if (!further)
                                {
                                    break;
                                }
                            }
                            pathinfo_ptr.children.Add(UsrPath);
                            cont = false;
                            break;
                        }
                    }
                    if (cont)
                    {
                        RawPath_Result[pair.Key] = UsrPath;
                    }
                }
            }

            return RawPath_Result.Values.ToList();
        }
        // Gets all of the path permissions in a well formatted way.
    }
}
