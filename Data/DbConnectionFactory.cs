using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cryobot.Data
{
    public class DbConnectionFactory
    {
        public SqlConnection Create()
        {
            return new SqlConnection(ConfigurationManager.ConnectionStrings["RecordsConnection"].ConnectionString);
        }
    }
}
