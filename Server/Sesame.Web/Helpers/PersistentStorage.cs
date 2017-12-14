//using System;
//using System.Collections.Generic;
//using System.Data.SqlClient;
//using System.Linq;
//using System.Security.Claims;
//using System.Threading.Tasks;

//namespace Sesame.Web
//{
//    // TODO move to somewhere that makes sense
//    public enum SpeakerProfileType
//    {
//        Identification = 1,
//        Verification
//    }

//    public static class PersistentStorage
//    {
//        internal static SqlConnection GetSqlConnection()
//        {
//            // Move to configuration


//            return null;
//        }

//        public static async Task CreateSpeakerProfileAsync(string aUserPrincipalName, SpeakerProfileType aSpeakerProfileType, string aProfileId)
//        {
//            using (SqlConnection sqlConnection = GetSqlConnection())
//            {
//                await sqlConnection.OpenAsync();

//                using (SqlCommand sqlCommand = new SqlCommand("INSERT INTO [SYSTEM_Speaker Profile] VALUES (@UserPrincipalName, @ProfileType, @ProfileId)", sqlConnection))
//                {
//                    sqlCommand.Parameters.AddWithValue("@UserPrincipalName", aUserPrincipalName);
//                    sqlCommand.Parameters.AddWithValue("@ProfileType", aSpeakerProfileType.ToString());
//                    sqlCommand.Parameters.AddWithValue("@ProfileId", aProfileId);

//                    try
//                    {
//                        await sqlCommand.ExecuteNonQueryAsync();
//                    }
//                    catch (Exception e)
//                    {
//                        throw new Exception($"Failed to create speaker profile: {e}");
//                    }
//                }
//            }
//        }
//        public static async Task<string> GetSpeakerProfileAsync(string aUserPrincipalName, SpeakerProfileType aSpeakerProfileType)
//        {
//            using (SqlConnection sqlConnection = GetSqlConnection())
//            {
//                await sqlConnection.OpenAsync();

//                using (SqlCommand sqlCommand = new SqlCommand("SELECT [Profile ID] FROM [SYSTEM_Speaker Profile] WHERE [User Principal Name] = @UserPrincipalName AND [Profile Type] = @ProfileType", sqlConnection))
//                {
//                    sqlCommand.Parameters.AddWithValue("@UserPrincipalName", aUserPrincipalName);
//                    sqlCommand.Parameters.AddWithValue("@ProfileType", aSpeakerProfileType.ToString());

//                    try
//                    {
//                        SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync();

//                        string result = null;

//                        if (sqlDataReader.HasRows)
//                        {
//                            await sqlDataReader.ReadAsync();

//                            result = sqlDataReader.GetSqlString(0).Value;
//                        }

//                        return result;
//                    }
//                    catch (Exception e)
//                    {
//                        //Do something nice
//                        throw e;
//                    }
//                }
//            }
//        }
//        public static async Task<int> EnrollSpeakerPin(string aUserPrincipalName)
//        {
//            using (SqlConnection sqlConnection = GetSqlConnection())
//            {
//                await sqlConnection.OpenAsync();

//                int pin;
//                using (SqlCommand sqlCommand = new SqlCommand("[PROCEDURE_Generate Speaker PIN]", sqlConnection) { CommandType = System.Data.CommandType.StoredProcedure })
//                {
//                    using (SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync())
//                    {
//                        if (sqlDataReader.HasRows)
//                        {
//                            await sqlDataReader.ReadAsync();
//                            pin = (int)sqlDataReader.GetSqlInt64(0).Value;
//                        }
//                        else
//                        {
//                            throw new InvalidOperationException("Could not generate valid PIN.");
//                        }
//                    }
//                }

//                using (SqlCommand sqlCommand = new SqlCommand("[PROCEDURE_Assign Speaker PIN]", sqlConnection) { CommandType = System.Data.CommandType.StoredProcedure })
//                {
//                    sqlCommand.Parameters.Add(new SqlParameter("@UPN", aUserPrincipalName));
//                    sqlCommand.Parameters.Add(new SqlParameter("@PIN", pin));

//                    await sqlCommand.ExecuteNonQueryAsync();
//                }

//                return pin;
//            }
//        }
//        public static async Task<int?> GetPinBySpeakerAsync(string aUserPrincipalName)
//        {
//            using (SqlConnection sqlConnection = GetSqlConnection())
//            {
//                await sqlConnection.OpenAsync();

//                int? result = null;
//                using (SqlCommand sqlCommand = new SqlCommand("[PROCEDURE_Get PIN By Speaker]", sqlConnection) { CommandType = System.Data.CommandType.StoredProcedure })
//                {
//                    sqlCommand.Parameters.Add(new SqlParameter("@UserPrincipalName", aUserPrincipalName));

//                    SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync();

//                    if (sqlDataReader.HasRows)
//                    {
//                        await sqlDataReader.ReadAsync();
//                        result = sqlDataReader.GetSqlInt32(0).Value;
//                    }

//                    return result;
//                }
//            }
//        }
//        public static async Task<string> GetSpeakerByPinAsync(int aPin)
//        {
//            using (SqlConnection sqlConnection = GetSqlConnection())
//            {
//                await sqlConnection.OpenAsync();

//                string result;
//                using (SqlCommand sqlCommand = new SqlCommand("[PROCEDURE_Get Speaker By PIN]", sqlConnection) { CommandType = System.Data.CommandType.StoredProcedure })
//                {
//                    sqlCommand.Parameters.Add(new SqlParameter("@PIN", aPin));

//                    SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync();

//                    if (sqlDataReader.HasRows)
//                    {
//                        await sqlDataReader.ReadAsync();
//                        result = sqlDataReader.GetSqlString(0).Value;
//                    }
//                    else
//                    {
//                        throw new InvalidOperationException("Speaker with that PIN does not exist.");
//                    }

//                    return result;
//                }
//            }
//        }
//        public static async Task<string> GetSpeakerVerificationProfileByPinAsync(int aPin)
//        {
//            return await GetSpeakerProfileAsync(await GetSpeakerByPinAsync(aPin), SpeakerProfileType.Verification);
//        }
//        public static async Task<string> GetSpeakerVerificationPhraseAsync(string aUserPrincipalName)
//        {
//            using (SqlConnection sqlConnection = GetSqlConnection())
//            {
//                await sqlConnection.OpenAsync();

//                string result = null;
//                using (SqlCommand sqlCommand = new SqlCommand("[PROCEDURE_Get Speaker Phrase]", sqlConnection) { CommandType = System.Data.CommandType.StoredProcedure })
//                {
//                    sqlCommand.Parameters.Add(new SqlParameter("@UPN", aUserPrincipalName));

//                    SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync();

//                    if (sqlDataReader.HasRows)
//                    {
//                        await sqlDataReader.ReadAsync();
//                        result = sqlDataReader.GetSqlString(0).Value;
//                    }

//                    return result;
//                }
//            }
//        }
//        public static async Task UpdateSpeakerVerificationPhraseAsync(string aUserPrincipalName, string aSpeakerVerificationPhrase)
//        {
//            using (SqlConnection sqlConnection = GetSqlConnection())
//            {
//                await sqlConnection.OpenAsync();
                
//                using (SqlCommand sqlCommand = new SqlCommand("[PROCEDURE_Update Speaker Phrase]", sqlConnection) { CommandType = System.Data.CommandType.StoredProcedure })
//                {
//                    sqlCommand.Parameters.Add(new SqlParameter("@UPN", aUserPrincipalName));
//                    sqlCommand.Parameters.Add(new SqlParameter("@Phrase", aSpeakerVerificationPhrase));

//                    await sqlCommand.ExecuteNonQueryAsync();
//                }
//            }
//        }
//        public static async Task<SimpleClaim> GetSimpleClaimAsync(string aUserPrincipalName)
//        {
//            using (SqlConnection sqlConnection = GetSqlConnection())
//            {
//                await sqlConnection.OpenAsync();

//                using (SqlCommand sqlCommand = new SqlCommand("SELECT [Object Identifier], [User Principal Name], [Given Name], [Surname] FROM [SYSTEM_Claims] WHERE [User Principal Name] = @UPN", sqlConnection))
//                {
//                    sqlCommand.Parameters.Add(new SqlParameter("@UPN", aUserPrincipalName));

//                    SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync();

//                    if (sqlDataReader.HasRows)
//                    {
//                        await sqlDataReader.ReadAsync();
//                        return new SimpleClaim()
//                        {
//                            ObjectIdentifier = sqlDataReader.GetSqlString(0).Value,
//                            UserPrincipalName = sqlDataReader.GetSqlString(1).Value,
//                            GivenName = sqlDataReader.GetSqlString(2).Value,
//                            Surname = sqlDataReader.GetSqlString(3).Value
//                        };
//                    }
//                    else
//                    {
//                        return null;
//                    }
//                }
//            }
//        }

//        public static async Task UpdateClaim(SimpleClaim aSimpleClaim)
//        {
//            using (SqlConnection sqlConnection = GetSqlConnection())
//            {
//                await sqlConnection.OpenAsync();

//                using (SqlCommand sqlCommand = new SqlCommand("[PROCEDURE_Update Claim]", sqlConnection) { CommandType = System.Data.CommandType.StoredProcedure })
//                {
//                    sqlCommand.Parameters.Add(new SqlParameter("@ObjectIdentifier", aSimpleClaim.ObjectIdentifier));
//                    sqlCommand.Parameters.Add(new SqlParameter("@UPN", aSimpleClaim.UserPrincipalName));
//                    sqlCommand.Parameters.Add(new SqlParameter("@GivenName", aSimpleClaim.GivenName));
//                    sqlCommand.Parameters.Add(new SqlParameter("@Surname", aSimpleClaim.Surname));

//                    await sqlCommand.ExecuteNonQueryAsync();
//                }
//            }
//        }
//    }
//}
