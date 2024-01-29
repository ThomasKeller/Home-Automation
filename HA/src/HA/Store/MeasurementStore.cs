namespace HA.Store;

public class MeasurementStore : IMeasurementStore
{
    private readonly string _databaseName;

    public MeasurementStore(string databaseName = "Measurement.db")
    {
        _databaseName = databaseName;
        Init();
    }

    private void Init()
    {
        using (var db = new DatabaseContext(_databaseName))
            db.Database.EnsureCreated();
    }

    public int Count()
    {
        using (var db = new DatabaseContext(_databaseName))
            return db.MeasurementEntities.Count();
    }

    public IEnumerable<MeasurementEntity> GetAll()
    {
        using (var db = new DatabaseContext(_databaseName))
        {
            var result = db.MeasurementEntities.ToArray();
            return result;
        }
    }

    public int Remove(int id)
    {
        using (var db = new DatabaseContext(_databaseName))
        {
            db.MeasurementEntities.Count();
            var entity = db.MeasurementEntities.Find(id);
            if (entity != null)
            {
                db.MeasurementEntities.Remove(entity);
                return db.SaveChanges();
            }
            return 0;
        }
    }

    public int Remove(IEnumerable<int> ids)
    {
        using (var db = new DatabaseContext(_databaseName))
        {
            foreach (var id in ids)
            {
                var entity = db.MeasurementEntities.Find(id);
                if (entity != null)
                {
                    db.MeasurementEntities.Remove(entity);
                }
            }
            return db.SaveChanges();
        }
    }

    public int Save(Measurement measurement)
    {
        using (var db = new DatabaseContext(_databaseName))
        {
            db.Add(measurement.ToEntity());
            return db.SaveChanges();
        }
    }

    public int Save(IEnumerable<Measurement> measurements)
    {
        //var index = 0;
        //var changes = 0;
        using (var db = new DatabaseContext(_databaseName))
        {
            foreach (var measurement in measurements)
            {
                db.Add(measurement.ToEntity());
            }
            return db.SaveChanges();
        }
    }
}