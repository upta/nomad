public static partial class Module
{
    [SpacetimeDB.View(Name = "ActiveEntities", Accessor = "ActiveEntities", Public = true)]
    public static List<Entity> ActiveEntities(AnonymousViewContext ctx)
    {
        return [.. ctx.Db.Entities.Active.Filter(true)];
    }
}
