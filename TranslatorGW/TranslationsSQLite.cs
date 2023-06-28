using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TranslatorGW.Configuration;

namespace TranslatorGW;

public class TranslationsSQLite : IDisposable
{
    private SqliteConnection _sql;
    private bool disposedValue;


    public TranslationsSQLite(string sqlitePath)
    {
        _sql = new SqliteConnection($"Data Source={sqlitePath};");
        _sql.Open();

        CreateTranslationTable();
    }


    private void CreateTranslationTable()
    {
        using (var cmd = new SqliteCommand(@"CREATE TABLE IF NOT EXISTS Translation (
                                        LanguageId INT NOT NULL,
                                        StringId INT NOT NULL,
                                        Text TEXT NOT NULL,
                                        PRIMARY KEY(LanguageId, StringId))", _sql))
        {
            cmd.ExecuteNonQuery();
        }
    }


    public void InsertTranslation(int languageId, int stringId, string text)
    {
        string insertDataQuery = @"INSERT OR IGNORE INTO Translation (LanguageId, StringId, Text) 
                           VALUES (@LanguageId, @StringId, @Text)";

        using (var cmd = new SqliteCommand(insertDataQuery, _sql))
        {
            cmd.Parameters.AddWithValue("@LanguageId", languageId);
            cmd.Parameters.AddWithValue("@StringId", stringId);
            cmd.Parameters.AddWithValue("@Text", text);

            cmd.ExecuteNonQuery();
        }

    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                _sql.Close();
                _sql.Dispose();
                // TODO: dispose managed state (managed objects)
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
