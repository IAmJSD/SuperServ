using Nancy.ModelBinding;
using System.IO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;

namespace SuperServ
{
    public class APIV1Module : Nancy.NancyModule
    {
        public static Nancy.Response AsErrorObject(Nancy.HttpStatusCode statusCode, object data)
        {
            return new Nancy.Response
            {
                StatusCode = statusCode,
                ContentType = "application/json",
                Contents = stream => (new StreamWriter(stream) { AutoFlush = true }).Write(JsonConvert.SerializeObject(data, Formatting.Indented))
            };
        }

        public static AuthCheckInternalResponse CheckAuthHeader(Nancy.NancyContext context)
        {
            var auth_header = context.Request.Headers.Authorization;
            if (auth_header == null)
            {
                return new AuthCheckInternalResponse()
                {
                    authed = false,
                    err = AsErrorObject(Nancy.HttpStatusCode.BadRequest, new
                    {
                        success = false,
                        type = "INVALID_CREDS",
                        message = "No valid authorization header."
                    })
                };
            }
            var auth_header_split = auth_header.Split(' ');
            if (auth_header_split.Length != 2 || auth_header_split[0].ToLower() != "bearer")
            {
                return new AuthCheckInternalResponse()
                {
                    authed = false,
                    err = AsErrorObject(Nancy.HttpStatusCode.BadRequest, new
                    {
                        success = false,
                        type = "INVALID_CREDS",
                        message = "Invalid authorization header."
                    })
                };
            }
            var auth_token = auth_header_split[1];
            foreach (KeyValuePair<string, AccessTokenInfo> access_token_pair in Program.config_handler.config.access_tokens)
            {
                if (access_token_pair.Key == auth_token)
                {
                    if ((int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds > access_token_pair.Value.active_until)
                    {
                        return new AuthCheckInternalResponse()
                        {
                            authed = false,
                            err = AsErrorObject(Nancy.HttpStatusCode.Forbidden, new
                            {
                                success = false,
                                type = "TOKEN_EXPIRED",
                                message = "Token has expired."
                            })
                        };
                    }
                    return new AuthCheckInternalResponse()
                    {
                        authed = true,
                        access_token_pair = access_token_pair
                    };
                }
            }
            return new AuthCheckInternalResponse()
            {
                authed = false,
                err = AsErrorObject(Nancy.HttpStatusCode.Forbidden, new
                {
                    success = false,
                    type = "INVALID_CREDS",
                    message = "Invalid token."
                })
            };
        }
        // Checks the Authorization header (https://www.youtube.com/watch?v=j8mVYEGtTGA).

        public static async Task<GenericAPIResponse> ChangePassword(UserEditPOST user_data)
        {
            if (user_data.new_password == "")
            {
                return new GenericAPIResponse()
                {
                    success = false,
                    type = "PWD_ERR",
                    message = "Your new password cannot be blank."
                };
            }
            if (Program.config_handler.config.password_complexity && !RegexCompilations.password_complexity_re.IsMatch(user_data.new_password))
            {
                return new GenericAPIResponse()
                {
                    success = false,
                    type = "PWD_ERR",
                    message = "Your new password is not complex enough."
                };
            }
            if (Program.config_handler.config.hibp_password_check && await new HIBP.PwnedPasswordApi("SuperServ").IsPasswordPwnedAsync(user_data.new_password) != 0)
            {
                return new GenericAPIResponse()
                {
                    success = false,
                    type = "PWD_ERR",
                    message = "The password given has been in a data breach that was indexed by <a href=\"https://haveibeenpwned.com\">Have I Been Pwned</a>."
                };
            }
            foreach (var token_kv_pair in Program.config_handler.config.access_tokens) {
                if (token_kv_pair.Value.user_id == user_data.user_uuid) {
                    Program.config_handler.config.access_tokens.Remove(token_kv_pair.Key);
                }
            }
            Program.config_handler.config.users[user_data.user_uuid].password = BCrypt.Net.BCrypt.HashPassword(user_data.new_password);
            Program.config_handler.config.users[user_data.user_uuid].must_change_password = false;
            await Task.Run(() => Program.config_handler.SaveConfig());
            return new GenericAPIResponse()
            {
                success = true,
                type = "SUCCESS",
                message = "Successfully changed password."
            };
        }
        // Changes the password.

        public static async Task<GenericAPIResponse> ChangeUsername(UserEditPOST user_data)
        {
            if (user_data.username == "")
            {
                return new GenericAPIResponse()
                {
                    success = false,
                    type = "USERNAME_ERR",
                    message = "Your new username cannot be blank."
                };
            }
            foreach (KeyValuePair<string, User> user_kv_pair in Program.config_handler.GetUsers())
            {
                if (user_kv_pair.Value.username.ToLower() == user_data.username.ToLower())
                {
                    return new GenericAPIResponse()
                    {
                        success = false,
                        type = "USERNAME_ERR",
                        message = "Your new username cannot be the same as another users or your own currently."
                    };
                }
            }
            Program.config_handler.config.users[user_data.user_uuid].username = user_data.username;
            await Task.Run(() => Program.config_handler.SaveConfig());
            return new GenericAPIResponse()
            {
                success = true,
                type = "SUCCESS",
                message = "Successfully changed username."
            };
        }
        // Changes the username.

        public static async Task<GenericAPIResponse> ChangeEmail(UserEditPOST user_data)
        {
            if (!RegexCompilations.email_re.IsMatch(user_data.email))
            {
                return new GenericAPIResponse()
                {
                    success = true,
                    type = "EMAIL_ERR",
                    message = "Invalid e-mail."
                };
            }
            Program.config_handler.config.users[user_data.user_uuid].email = user_data.email;
            Program.config_handler.config.users[user_data.user_uuid].email_verified = false;
            await Task.Run(() => Program.config_handler.SaveConfig());
            return new GenericAPIResponse()
            {
                success = true,
                type = "SUCCESS",
                message = "Successfully changed e-mail."
            };
        }
        // Changes the e-mail.

        public APIV1Module() : base("/api/v1")
        {
            Post("/auth", async _ => {
                AuthPOST user_data = this.Bind<AuthPOST>();
                if (user_data.username == null || user_data.password == null)
                {
                    return AsErrorObject(Nancy.HttpStatusCode.BadRequest, new
                    {
                        success = false,
                        type = "BAD_REQUEST",
                        message = "Not all fields present."
                    });
                }
                foreach (var user in Program.config_handler.GetUsers())
                {
                    if (user.Value.username.ToLower() == user_data.username.ToLower())
                    {
                        var password_check = BCrypt.Net.BCrypt.Verify(user_data.password, user.Value.password);
                        if (!password_check)
                        {
                            return AsErrorObject(Nancy.HttpStatusCode.Forbidden, new
                            {
                                success = false,
                                type = "INVALID_CREDS",
                                message = "Username or password is invalid."
                            });
                        }
                        if (user.Value.disabled)
                        {
                            return AsErrorObject(Nancy.HttpStatusCode.Forbidden, new
                            {
                                success = false,
                                type = "USR_DISABLED",
                                message = "Your user account is disabled. Please contact the server administrator for more information."
                            });
                        }
                        if (user.Value.must_change_password)
                        {
                            return AsErrorObject(Nancy.HttpStatusCode.Forbidden, new
                            {
                                success = true,
                                type = "USR_MUST_CHANGE_PASSWORD",
                                user_uuid = user.Key,
                                message = "Your password must be changed in order to continue."
                            });
                        }
                        string AccessToken = Guid.NewGuid().ToString();
                        int expires_in = 172800;
                        int unix_timestamp = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                        Program.config_handler.config.users[user.Key].last_login = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                        Program.config_handler.config.access_tokens.Add(AccessToken, new AccessTokenInfo()
                        {
                            active_until = unix_timestamp + expires_in,
                            user_id = user.Key
                        });
                        await Task.Run(() => Program.config_handler.SaveConfig());
                        return new
                        {
                            success = true,
                            type = "SUCCESS",
                            token = AccessToken,
                            user_uuid = user.Key,
                            expires_in = expires_in
                        };
                    }
                }
                return AsErrorObject(Nancy.HttpStatusCode.Forbidden, new
                {
                    success = false,
                    type = "INVALID_CREDS",
                    message = "Username or password is invalid."
                });
            });
            // Handles authentication.

            Post("/user/edit", async _ => {
                UserEditPOST user_data = this.Bind<UserEditPOST>();
                if (user_data.password == null || user_data.user_uuid == null)
                {
                    return await AsErrorObject(Nancy.HttpStatusCode.BadRequest, new
                    {
                        success = false,
                        type = "BAD_REQUEST",
                        message = "Password/user UUID fields need to be present."
                    });
                }
                User user;
                try
                {
                    user = Program.config_handler.config.users[user_data.user_uuid];
                } catch (KeyNotFoundException) {
                    return await AsErrorObject(Nancy.HttpStatusCode.Forbidden, new
                    {
                        success = false,
                        type = "INVALID_CREDS",
                        message = "User UUID or password is invalid."
                    });
                }
                var password_check = BCrypt.Net.BCrypt.Verify(user_data.password, user.password);
                if (!password_check)
                {
                    return await AsErrorObject(Nancy.HttpStatusCode.Forbidden, new
                    {
                        success = false,
                        type = "INVALID_CREDS",
                        message = "User UUID or password is invalid."
                    });
                }
                if (user.disabled)
                {
                    return await AsErrorObject(Nancy.HttpStatusCode.Forbidden, new
                    {
                        success = false,
                        type = "USR_DISABLED",
                        message = "Your user account is disabled. Please contact the server administrator for more information."
                    });
                }

                UserEditResponse response = new UserEditResponse()
                {
                    requested_edits = new RequestedUserEdits(),
                    success = true,
                    type = "SUCCESS"
                };

                bool allFail = true;

                if (user_data.new_password != null)
                {
                    var r = await ChangePassword(user_data);
                    if (!r.success)
                    {
                        response.type = "PARTIAL_SUCCESS";
                        response.success = false;
                    } else {
                        allFail = false;
                    }
                    response.requested_edits.new_password = r;
                }

                if (user_data.username != null)
                {
                    var r = await ChangeUsername(user_data);
                    if (!r.success)
                    {
                        response.type = "PARTIAL_SUCCESS";
                        response.success = false;
                    }
                    else
                    {
                        allFail = false;
                    }
                    response.requested_edits.username = r;
                }

                if (user_data.email != null)
                {
                    var r = await ChangeEmail(user_data);
                    if (!r.success)
                    {
                        response.type = "PARTIAL_SUCCESS";
                        response.success = false;
                    }
                    else
                    {
                        allFail = false;
                    }
                    response.requested_edits.email = r;
                }

                if (allFail)
                {
                    if (response.success)
                    {
                        response.type = "NO_CONTENT";
                    } else {
                        response.type = "ALL_FAIL";
                    }
                    
                }

                if (response.success)
                {
                    return response;
                }
                return await AsErrorObject(Nancy.HttpStatusCode.BadRequest, response);
            });
            // Allows the user to be edited.

            Get("/user/paths", async _ =>
            {
                AuthCheckInternalResponse AuthHeaderCheck = CheckAuthHeader(Context);
                if (AuthHeaderCheck.authed)
                {
                    User user = Program.config_handler.config.users[AuthHeaderCheck.access_token_pair.Value.user_id];
                    if (user.disabled)
                    {
                        return await AsErrorObject(Nancy.HttpStatusCode.BadRequest, new
                        {
                            success = false,
                            type = "USR_DISABLED",
                            message = "Your user account is disabled. Please contact the server administrator for more information."
                        });
                    }
                    return Program.config_handler.GetPathPerms(AuthHeaderCheck.access_token_pair.Key, user);
                }
                else
                {
                    return await AuthHeaderCheck.err;
                }
            });
            // Displays all of the paths.

            Get("/user/path/{path}", async args =>
            {
                AuthCheckInternalResponse AuthHeaderCheck = CheckAuthHeader(Context);
                if (!AuthHeaderCheck.authed)
                {
                    return await AuthHeaderCheck.err;
                }
                string b64;
                try
                {
                    b64 = Encoding.UTF8.GetString(Convert.FromBase64String(args.path));
                }
                catch (Exception)
                {
                    return await AsErrorObject(Nancy.HttpStatusCode.BadRequest, new
                    {
                        success = false,
                        type = "BAD_B64",
                        message = "Couldn't decode the Base 64 given. Make sure it is Base 64 encoded UTF-8."
                    });
                }
                User user = Program.config_handler.config.users[AuthHeaderCheck.access_token_pair.Value.user_id];
                if (user.disabled)
                {
                    return await AsErrorObject(Nancy.HttpStatusCode.BadRequest, new
                    {
                        success = false,
                        type = "USR_DISABLED",
                        message = "Your user account is disabled. Please contact the server administrator for more information."
                    });
                }
                return Utils.GetPath(AuthHeaderCheck.access_token_pair.Key, user, b64);
            });
            // Displays more info about a specific path.
        }
    }
}
