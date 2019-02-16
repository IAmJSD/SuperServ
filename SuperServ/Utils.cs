using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace SuperServ
{
    public class UniquehighestPermDict: Dictionary<string, PathInfo>
    {
        public void Add(string key, PathInfo value, bool _override = false)
        {
            try
            {
                var item = this[key];
                if (_override)
                {
                    this[key].delete_folder = value.delete_folder;
                    this[key].delete_inside = value.delete_inside;
                    this[key].read = value.read;
                    this[key].write = value.write;
                }
                else
                {
                    if (!item.delete_folder && value.delete_folder)
                    {
                        this[key].delete_folder = true;
                    }
                    if (!item.delete_inside && value.delete_inside)
                    {
                        this[key].delete_inside = true;
                    }
                    if (!item.read && value.read)
                    {
                        this[key].read = true;
                    }
                    if (!item.write && value.write)
                    {
                        this[key].write = true;
                    }
                }
            }
            catch (Exception) {
                this[key] = value;
            }
        }
    }
    // Makes a unique dictionary of items, makes permissions the highest they can be.

    public class Utils
    {
        public static BasicFileFolderInfo GetFile(string uid, User user, string path)
        {
            char SlashType = '/';

            if (path.EndsWith(SlashType))
            {
                path.TrimEnd(SlashType);
            }

            var path_slash_split = path.Split(SlashType);
            if (path_slash_split.Length < 2)
            {
                return null;
            }

            var file = path_slash_split[path_slash_split.Length - 1];
            var path_slash_split_list = path_slash_split.ToList();
            path_slash_split_list.RemoveAt(path_slash_split.Length - 1);

            var path_ = GetPath(uid, user, String.Join(SlashType, path_slash_split_list));

            if (path_ == null)
            {
                return null;
            }

            if (!path_.read)
            {
                return null;
            }

            foreach (var _file in path_.files)
            {
                if (_file.name == file)
                {
                    return _file;
                }
            }

            return null;
        }
        // Returns the file if the user should be able to see it.

        public static UserPathExt GetPath(string uid, User user, string path)
        {
            char SlashType = '/';

            if (path.EndsWith(SlashType))
            {
                path.TrimEnd(SlashType);
            }

            List<UserPath> allowed_paths = Program.config_handler.GetPathPerms(uid, user);

            foreach (UserPath usr_path in allowed_paths)
            {
                if (!path.ToLower().TrimStart('/').StartsWith(usr_path.real_path.TrimStart('/').ToLower()))
                {
                    continue;
                }

                UserPath end_path = usr_path;
                bool match = false;
                while (true)
                {
                    bool further = false;
                    if (end_path.real_path.TrimStart('/').ToLower() == path.TrimStart('/').ToLower())
                    {
                        match = true;
                        break;
                    }
                    foreach (UserPath child in end_path.children)
                    {
                        if (path.TrimStart('/').ToLower().StartsWith(child.real_path.TrimStart('/').ToLower()))
                        {
                            further = true;
                            end_path = child;
                            break;
                        }
                    }
                    if (!further)
                    {
                        break;
                    }
                }
                if (!match)
                {
                    var path_split = path.Split(SlashType);
                    end_path = new UserPath()
                    {
                        children = end_path.children,
                        delete_folder = end_path.delete_folder,
                        delete_inside = end_path.delete_inside,
                        name = path_split[path_split.Length - 1],
                        read = end_path.read,
                        write = end_path.write,
                        real_path = path
                    };
                }

                UserPathExt path_ext = new UserPathExt()
                {
                    children = end_path.children,
                    delete_folder = end_path.delete_folder,
                    delete_inside = end_path.delete_inside,
                    name = end_path.name,
                    read = end_path.read,
                    real_path = end_path.real_path,
                    write = end_path.write,
                    files = new List<BasicFileFolderInfo>(),
                    folders = new List<BasicFileFolderInfo>()
                };

                try {
                    string[] Files = System.IO.Directory.GetFiles(path_ext.real_path, "*", System.IO.SearchOption.TopDirectoryOnly);
                    string[] Directories = System.IO.Directory.GetDirectories(path_ext.real_path, "*", System.IO.SearchOption.TopDirectoryOnly);

                    foreach (string file in Files)
                    {
                        string[] fsplit = file.Split(SlashType);
                        path_ext.files.Add(new BasicFileFolderInfo()
                        {
                            name = fsplit[fsplit.Length - 1],
                            path = file
                        });
                    }

                    foreach (string dir in Directories)
                    {
                        if (dir.EndsWith(SlashType))
                        {
                            dir.TrimEnd(SlashType);
                        }
                        string[] dsplit = dir.Split(SlashType);
                        path_ext.folders.Add(new BasicFileFolderInfo()
                        {
                            name = dsplit[dsplit.Length - 1],
                            path = dir
                        });
                    }
                } catch(Exception) {
                    return null;
                }

                return path_ext;
            }

            return null;
        }
        // Returns the path if the user should be able to see it.
    }
}
