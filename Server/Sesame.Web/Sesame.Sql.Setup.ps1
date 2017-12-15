Param($ServerInstance = "(localdb)\v11.0")

Invoke-Sqlcmd -Query "CREATE DATABASE SesameCache" -ServerInstance $ServerInstance

$Command = "Data Source={0};Initial Catalog=SesameCache;Integrated Security=True;" -f $ServerInstance

dotnet sql-cache create $Command dbo Cache

Invoke-Sqlcmd -Query "CREATE DATABASE SesameSessionState" -ServerInstance $ServerInstance

$Command = "Data Source={0};Initial Catalog=SesameSessionState;Integrated Security=True;" -f $ServerInstance

dotnet sql-cache create $Command dbo State