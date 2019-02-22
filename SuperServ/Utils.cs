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

            var PathSplit = path.TrimStart(SlashType).Split(SlashType).ToList();

            if (PathSplit.Count == 0) {
                return null;
            }

            foreach (UserPath usr_path in allowed_paths)
            {
                if (PathSplit[0].ToLower() != usr_path.name.ToLower())
                {
                    continue;
                }

                // This starts with the allowed path.

                PathSplit.RemoveAt(0);
                // Lets remove this part. We only care about the children in this now.

                UserPath end_path = usr_path;
                // This defines the end path.

                bool Inherit = false;
                // Defines whether to inherit permissions.

                while (PathSplit.Count != 0) {
                    // This will cycle while the list isn't empty.

                    string NextChild = PathSplit[0];
                    // Defines the next child.

                    foreach (UserPath child in end_path.children)
                    {
                        if (child.name.ToLower() == NextChild.ToLower()) {
                            // This child is the next match, lets make this the end path and break this for loop.
                            end_path = child;
                            Inherit = true;
                            break;
                        }
                    }

                    if (Inherit) {
                        // We are NOT further despite cycling through all of the children, time to break and inherit.
                        break;
                    }

                    PathSplit.RemoveAt(0);
                    // Removes the first item from the array.
                }

                if (Inherit) {
                    // This is where we actually handle inheriting the permissions.
                    end_path = new UserPath() {
                        children = new List<UserPath>(),
                        delete_folder = end_path.delete_folder,
                        delete_inside = end_path.delete_inside,
                        name = PathSplit[PathSplit.Count - 1],
                        read = end_path.read,
                        write = end_path.write,
                        real_path = end_path.real_path + SlashType + String.Join(SlashType, PathSplit)
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
