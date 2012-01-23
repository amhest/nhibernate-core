using System;
using System.Collections.Generic;
using NHibernate.Criterion;
using NHibernate.Mapping.ByCode;
using NHibernate.Transform;
using NUnit.Framework;

namespace NHibernate.Test.NHSpecificTest.NH3138
{
	[TestFixture]
	public class FixtureByCode : TestCaseMappingByCode
	{
		protected override bool AppliesTo(Dialect.Dialect dialect)
		{
			return dialect is Dialect.MsSql2005Dialect;
		}

		protected override Cfg.MappingSchema.HbmMapping GetMappings()
		{
			var mapper = new ModelMapper();
			mapper.Class<Entity>(ca =>
			{
				ca.Lazy(false);
				ca.Id(x => x.Id, map => map.Generator(Generators.GuidComb));
				ca.Property(x => x.EnglishName);
				ca.Property(x => x.GermanName);
			});

			return mapper.CompileMappingForAllExplicitlyAddedEntities();
		}

		[Test]
		public void PageQueryWithDistinctAndOrderByContainingFunctionWithCommaSeparatedParameters()
		{
			using (var session = OpenSession())
			{
				Assert.DoesNotThrow(() =>
					session
						.CreateQuery("select distinct e.Id, coalesce(e.EnglishName, e.GermanName) from Entity e order by coalesce(e.EnglishName, e.GermanName) asc")
						.SetFirstResult(10)
						.SetMaxResults(20)
						.List());
			}
		}

      [Test]
      [Ignore("Failing")] //This is failing because HQL -> SQL is failing to translate the alias in Order By
	   public void PageQueryWithDistinctAndOrderByContainingAliasedFunction()
	   {
	      using (var session = OpenSession())
	      {
	         Assert.DoesNotThrow(() =>
	                             session
	                                .CreateQuery(
	                                   "select distinct e.Id, coalesce(e.EnglishName, e.GermanName) as LocalizedName from Entity e order by LocalizedName asc")
	                                .SetFirstResult(10)
	                                .SetMaxResults(20)
	                                .List<Entity>());
	      }
	   }

	   [Test]
	   public void PageQueryOverWithDistinctAndOrderByContainingAliasedFunction()
	   {
	      using (ISession session = OpenSession())
	      {
	         String LocalizedName = "LocalizedName";
	         //Build a set of columns with a coalese
	         ProjectionList plColumns = Projections.ProjectionList();
	         plColumns.Add(Projections.Property<Entity>(x => x.Id), "Id");
	         plColumns.Add(Projections.SqlFunction("coalesce",
	                                               NHibernateUtil.String,
	                                               Projections.Property<Entity>(x => x.EnglishName),
	                                               Projections.Property<Entity>(x => x.GermanName))
	                                  .WithAlias(() => LocalizedName));

	         ProjectionList plDistinct = Projections.ProjectionList();
	         plDistinct.Add(Projections.Distinct(plColumns));


	         //Make sure we parse and run without error
	         Assert.DoesNotThrow(() => session.QueryOver<Entity>()
	                                          .Select(plDistinct)
	                                          .TransformUsing(Transformers.AliasToBean<LocalizedEntity>())
	                                          .OrderByAlias(() => LocalizedName).Asc
	                                          .Skip(10)
	                                          .Take(20)
	                                          .List<LocalizedEntity>());


	         //Now make sure we actually page and limit
	         IList<LocalizedEntity> p1 = session.QueryOver<Entity>()
	                                            .Select(plDistinct)
	                                            .TransformUsing(Transformers.AliasToBean<LocalizedEntity>())
	                                            .OrderByAlias(() => LocalizedName).Asc
	            //
	            // First Page
	            //.Skip(10)
	                                            .Take(20)
	                                            .List<LocalizedEntity>();

	         Assert.IsNotNull(p1);

	         IList<LocalizedEntity> p2 = session.QueryOver<Entity>()
	                                            .Select(plDistinct)
	                                            .TransformUsing(Transformers.AliasToBean<LocalizedEntity>())
	                                            .OrderByAlias(() => LocalizedName).Asc
	            // Second Page
	                                            .Skip(10)
	                                            .Take(20)
	                                            .List<LocalizedEntity>();

	         Assert.IsNotNull(p2);
	         Assert.IsTrue(p1.Count == 20);
	         Assert.IsTrue(p2.Count == 20);

	         Assert.NotNull(p1[0]);
	         Assert.NotNull(p2[0]);

	         Assert.AreNotEqual(p1[0].Id, p2[0].Id);
	         Assert.AreNotEqual(p1[0].LocalizedName, p2[0].LocalizedName);
	      }
	   }

	   [SetUp]
	   public void SetUp()
	   {
	      using (ISession session = OpenSession())
	      {
	         using (ITransaction t = session.BeginTransaction())
	         {
	            for (int i = 0; i < (data.Length/2); i++)
	            {
	               var e = new Entity
	                  {
	                     Id = Guid.NewGuid(),
	                     EnglishName = data[i, 0],
	                     GermanName = data[i, 1]
	                  };
	               session.Save(e);
	            }
	            t.Commit();
	         }
	      }
	   }

	   [TearDown]
	   public void TearDown()
	   {
	      using (ISession session = OpenSession())
	      {
	         using (ITransaction t = session.BeginTransaction())
	         {
	            //Truncate
	            session.Delete("from Entity");
	            t.Commit();
	         }
	      }
	   }

	   private readonly string[,] data =
	      {
	         {"Faber", null},
	         {null, "dyer"},
	         {"Fassbinder", "cooper"},
	         {"Faust", "fist"},
	         {"Feierabend", null},
	         {"Fenstermacher", "window maker"},
	         {"Fiedler", "fiddler"},
	         {null, "finch"},
	         {"Fleischer", "butcher"},
	         {"Foerster", "forester"},
	         {"Frankfurter", "of Frankfurt"},
	         {null, "Friday"},
	         {"Freud", "joy"},
	         {"Frei", null},
	         {"Freeh", null},
	         {"Fruehauf", "up early"},
	         {"Fuchs", "fox"},
	         {"Fuerst/Furst", "prince"},
	         {"Fuhrmann", "carter, driver"},
	         {null, "gardener"},
	         {"Gerber", "tanner"},
	         {"Gerste/Gersten", "barley"},
	         {"Gloeckner/Glockner", "bell man"},
	         {"Gottschalk", "God's servant"},
	         {null, "green forest"},
	         {"Hertz/Herz", "heart"},
	         {"Hertzog/Herzog", "duke"},
	         {null, "heaven"},
	         {"Hirsch", "buck, deer"},
	         {"Hoch", null},
	         {"Jaeger", null},
	         {"Jung", "young"},
	         {"Junker", "nobleman, squire"},
	         {"Kaiser", "emperor"},
	         {"Kalb", "calf"},
	         {null, "cabinet maker"},
	         {"Kappel", "chapel"},
	         {"Kaufmann", "merchant"},
	         {"Kirsch", "cherry"},
	         {"Klein", "short, small"},
	         {"Klug/Kluge", null},
	         {"Koch", "cook"},
	         {null, "charcoal-maker"},
	         {"Koenig/Konig", "king"},
	         {"Krause", "curly haired"},
	         {"Kuefer", "cooper"},
	         {null, "sexton"},
	         {"Kuhn/Kunze", null}
	      };
	}

   internal class Entity
   {
      public Guid Id { get; set; }
      public string EnglishName { get; set; }
      public string GermanName { get; set; }
   }


   internal class LocalizedEntity
   {
      public Guid Id { get; set; }
      public string LocalizedName { get; set; }
   }
}