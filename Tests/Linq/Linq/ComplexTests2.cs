﻿using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;
using Tests.Model;


namespace Tests.ComplexTests2
{
	/// <summary>
	/// Tests:
	/// 
	/// Complex Property Mapping
	/// Inheritance Mapping
	/// LoadWith for Inheritance
	/// LoadWith with Casts
	/// Nested LoadWith
	/// String Enums
	/// Converters for Enums
	/// </summary>
	[TestFixture]
	public class ComplexTests2 : TestBase
	{
		private static int _cnt;

		public enum AnimalType
		{
			Small,
			Big
		}

		public enum AnimalType2
		{
			Small,
			Big
		}

		public class Eye
		{
			public int    Id { get; set; }
			public string Xy { get; set; }
		}

		public class SauronsEye : Eye
		{
			public int Power { get; set; }
		}

		public class Name
		{
			public string First  { get; set; }
			public string Second { get; set; }
		}

		public class Animal
		{
			public AnimalType  AnimalType    { get; set; }
			public AnimalType2 AnimalType2   { get; set; }
			public int         Id            { get; set; }
			public string      Name          { get; set; }
			public string      Discriminator { get; set; }
		}

		public class WildAnimal : Animal
		{
			public WildAnimal()
			{
				Discriminator = "WildAnimal";
			}
		}

		public class SuperWildAnimal : WildAnimal
		{
			public SuperWildAnimal()
			{
				Discriminator = "SuperWildAnimal";
			}
		}

		public class Dog : SuperWildAnimal
		{
			public Dog()
			{
				Discriminator = "Dog";
			}

			public Eye  Bla     { get; set; }
			public int? EyeId   { get; set; }
			public Name DogName { get; set; }
		}

		public class BadDog : Dog
		{
		}

		public class SuperBadDog : BadDog
		{
		}

		public class Test
		{
			public int Id { get; set; }
			public int? TestAnimalId { get; set; }
			public Animal TestAnimal { get; set; }
		}

		private void InsertData(ITestDataContext db)
		{
			CleanupData(db);

			var eye = new Eye
			{
				Id = 1,
				Xy = "Hallo"
			};

			var dog = new Dog
			{
				Id = 1,
				Discriminator = "Dog",
				EyeId = 1,
				Name = "FirstDog",
				DogName = new Name { First = "a", Second = "b" },
				AnimalType = AnimalType.Big,
				AnimalType2 = AnimalType2.Big
			};

			var test = new Test
			{
				Id = 1,
				TestAnimalId = null
			};

			var test2 = new Test
			{
				Id = 2,
				TestAnimalId = 1
			};

			db.CreateTable<Animal>();
			db.CreateTable<Eye>();
			db.CreateTable<Test>();

			db.Insert(eye);
			db.Insert(dog);
			db.Insert(test);
			db.Insert(test2);
		}

		void CleanupData(ITestDataContext db)
		{
			db.DropTable<Animal>(throwExceptionIfNotExists: false);
			db.DropTable<Eye>   (throwExceptionIfNotExists: false);
			db.DropTable<Test>  (throwExceptionIfNotExists: false);
		}

		MappingSchema SetMappings()
		{
			// counter added to fix this issue with tests in Firebird
			// https://stackoverflow.com/questions/44353607
			// it was only working solution for Firebird3
			var cnt = Interlocked.Increment(ref _cnt).ToString();

			var ms = new MappingSchema(cnt);
			ms.SetConverter<AnimalType, string>((obj) =>
			{
				return obj.ToString();
			});
			ms.SetConverter<AnimalType, DataParameter>((obj) =>
			{
				return new DataParameter { Value = obj.ToString() };
			});
			ms.SetConverter<string, AnimalType>((txt) =>
			{
				return (AnimalType)Enum.Parse(typeof(AnimalType), txt, true);
			});
			ms.SetDefaultFromEnumType(typeof(AnimalType2), typeof(string));

			var animalsTableName = "Animals" + cnt;

			var mappingBuilder = ms.GetFluentMappingBuilder();
			mappingBuilder.Entity<Animal>()
				.HasTableName(animalsTableName)
				.Inheritance(x => x.Discriminator, "Dog",             typeof(Dog))
				.Inheritance(x => x.Discriminator, "WildAnimal",      typeof(WildAnimal))
				.Inheritance(x => x.Discriminator, "SuperWildAnimal", typeof(SuperWildAnimal))
				.Property(x => x.Name         ).IsColumn().IsNullable().HasColumnName("Name")
				.Property(x => x.AnimalType   ).IsColumn().HasColumnName("AnimalType").HasDataType(DataType.NVarChar).HasLength(40)
				.Property(x => x.AnimalType2  ).IsColumn().HasColumnName("AnimalType2").HasDataType(DataType.NVarChar).HasLength(40)
				.Property(x => x.Discriminator).IsDiscriminator().IsColumn().IsNullable(false).HasColumnName("Discriminator").HasDataType(DataType.NVarChar).HasLength(40)
				.Property(x => x.Id           ).IsColumn().IsNullable(false).HasColumnName("Id").IsPrimaryKey();

			mappingBuilder.Entity<Dog>()
				.HasTableName(animalsTableName)
				.Property(x => x.Bla           ).IsNotColumn()
				.Property(x => x.EyeId         ).IsColumn().IsNullable().HasColumnName("EyeId")
				.Property(x => x.DogName.Second).HasColumnName("Second").HasDataType(DataType.NVarChar).HasLength(40)
				.Property(x => x.DogName.First ).HasColumnName("First").HasDataType(DataType.NVarChar).HasLength(40)
				.Association(x => x.Bla, x => x.EyeId, x => x.Id);

			mappingBuilder.Entity<WildAnimal>()
				.HasTableName(animalsTableName);

			mappingBuilder.Entity<SuperWildAnimal>()
				.HasTableName(animalsTableName);

			mappingBuilder.Entity<Eye>()
				.HasTableName("Eyes")
				.Property(x => x.Id).IsColumn().HasColumnName("Id").IsPrimaryKey()
				.Property(x => x.Xy).IsColumn().IsNullable().HasColumnName("Xy").HasDataType(DataType.NVarChar).HasLength(40);

			mappingBuilder.Entity<SauronsEye>()
				.HasTableName("Eyes")
				.Property(x => x.Id).IsColumn().HasColumnName("Id").IsPrimaryKey()
				.Property(x => x.Xy).IsColumn().IsNullable().HasColumnName("Xy").HasDataType(DataType.NVarChar).HasLength(40)
				.Property(x => x.Power).IsColumn().HasColumnName("power");

			mappingBuilder.Entity<Test>()
				.HasTableName("TestAnimalTable")
				.Association(x => x.TestAnimal, x => x.TestAnimalId, x => x.Id)
				.Property(x => x.TestAnimalId).IsColumn().IsNullable().HasColumnName("TestAnimalId")
				.Property(x => x.TestAnimal  ).IsNotColumn();

			return ms;
		}

		[Test, DataContextSource]
		public void TestQueryForBaseType(string context)
		{
			var ms = SetMappings();

			using (var db = GetDataContext(context, ms))
			{
				try
				{
					InsertData(db);
					var data =  db.GetTable<Animal>().ToList();
					Assert.Null(((Dog)data.First()).Bla);
				}
				finally
				{
					CleanupData(db);
				}
			}
		}

		[Test, DataContextSource]
		public void TestLoadWithWithCast(string context)
		{
			var ms = SetMappings();

			using (var db = GetDataContext(context, ms))
			{
				try
				{
					InsertData(db);
					var data = db.GetTable<Animal>().LoadWith(x => ((Dog)x).Bla).ToList();
					Assert.NotNull(((Dog)data.First()).Bla);
				}
				finally
				{
					CleanupData(db);
				}
			}
		}

		[Test, DataContextSource]
		public void TestNestedLoadWithWithCast(string context)
		{
			var ms = SetMappings();

			using (var db = GetDataContext(context, ms))
			{
				try
				{
					InsertData(db);
					var data = db.GetTable<Test>()
						.LoadWith(x => ((Dog)x.TestAnimal).Bla)
						.OrderBy(x => x.Id)
						.ToList();

					Assert.Null(data.First().TestAnimal);
					Assert.NotNull(((Dog)data.Skip(1).First().TestAnimal).Bla);
					Assert.NotNull(((Dog)data.Skip(1).First().TestAnimal).DogName.First);
					Assert.NotNull(((Dog)data.Skip(1).First().TestAnimal).DogName.Second);
				}
				finally
				{
					CleanupData(db);
				}
			}
		}

		[Test, DataContextSource]
		public void TestComplexPropertyLoading(string context)
		{
			var ms = SetMappings();

			using (var db = GetDataContext(context, ms))
			{
				try
				{
					InsertData(db);
					var data = db.GetTable<Dog>().ToList();

					Assert.NotNull(data[0].DogName.First);
					Assert.NotNull(data[0].DogName.Second);
				}
				finally
				{
					CleanupData(db);
				}
			}
		}

		[Test, DataContextSource]
		public void TestStringAndConverterEnums(string context)
		{
			var ms = SetMappings();

			using (var db = GetDataContext(context, ms))
			{
				try
				{
					InsertData(db);
					var d = new Dog() { AnimalType = AnimalType.Big, AnimalType2 = AnimalType2.Big };

					var test1 = db.GetTable<Dog>().First(x => x.AnimalType == AnimalType.Big);
					var test2 = db.GetTable<Dog>().First(x => x.AnimalType == d.AnimalType);

					var test3 = db.GetTable<Dog>().First(x => x.AnimalType2 == AnimalType2.Big);
					var test4 = db.GetTable<Dog>().First(x => x.AnimalType2 == d.AnimalType2);

					var test6 = db.GetTable<Animal>().First(x => x is SuperWildAnimal);

					var test7 = db.GetTable<Test>().First(x => x.TestAnimal is Dog && ((Dog)x.TestAnimal).EyeId == 1);
				}
				finally
				{
					CleanupData(db);
				}
			}
		}

		[Test, DataContextSource]
		public void TestUpdateWithTypeAndBaseType(string context)
		{
			var ms = SetMappings();

			using (var db = GetDataContext(context, ms))
			{
				try
				{
					InsertData(db);
					var dog = db.GetTable<Dog>().First();
					db.Update(dog);
					db.Update((Animal)dog);

					var bdog = new SuperBadDog();
					db.Insert((Dog)bdog);
				}
				finally
				{
					CleanupData(db);
				}
			}
		}

		public class PersonDerived : Person
		{
			[Column]
			public int ColumnForOtherDB;
		}

		[Test, DataContextSource]
		public void TestInsertUsingDerivedObjectUsingAttributes(string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					Person person = new PersonDerived()
					{
						FirstName        = "test_inherited_insert",
						LastName         = "test",
						MiddleName       = "test",
						Gender           = Gender.Unknown,
						ColumnForOtherDB = 100500
					};

					person.ID = db.InsertWithInt32Identity(person);
					validate();
					db.Update(person);
					validate();
					db.Delete(person);

					void validate()
					{
						var data = db.GetTable<Person>().Where(_ => _.FirstName == "test_inherited_insert").FirstOrDefault();
						Assert.IsNotNull(data);
						Assert.AreEqual(person.ID        , data.ID);
						Assert.AreEqual(person.FirstName , data.FirstName);
						Assert.AreEqual(person.LastName  , data.LastName);
						Assert.AreEqual(person.MiddleName, data.MiddleName);
						Assert.AreEqual(person.Gender    , data.Gender);
					}
				}
				finally
				{
					db.GetTable<Person>().Where(_ => _.FirstName == "test_inherited_insert").Delete();
				}
			}
		}

		[Test, DataContextSource]
		public void TestInsertUsingDerivedObjectUsingFluentMapping(string context)
		{
			var ms = SetMappings();

			using (var db = GetDataContext(context, ms))
			{
				try
				{
					InsertData(db);

					Eye eye = new SauronsEye()
					{
						Id = 123,
						Xy = "test321"
					};

					var cnt = db.Insert(eye);
					validate();
					cnt = db.InsertOrReplace(eye);
					validate();
					cnt = db.Update(eye);
					validate();
					db.Delete(eye);

					void validate()
					{
						Assert.AreEqual(1, cnt);

						var data = db.GetTable<Eye>().Where(_ => _.Id == 123).FirstOrDefault();
						Assert.IsNotNull(data);
						Assert.AreEqual(eye.Id, data.Id);
						Assert.AreEqual(eye.Xy, data.Xy);
					}
				}
				finally
				{
					CleanupData(db);
				}
			}
		}

		[Test, DataContextSource]
		public void TestInheritanceByBaseType(string context)
		{
			var ms = SetMappings();

			using (var db = GetDataContext(context, ms))
			{
				try
				{
					InsertData(db);

					var dog = new Dog()
					{
						Id = 666,
						AnimalType = AnimalType.Big,
						AnimalType2 = AnimalType2.Small,
						Name = "Cerberus",
						DogName = new Name()
						{
							First = "Good",
							Second = "Dog"
						},
						EyeId = 2
					};

					var cnt = db.Insert((Animal)dog);
					validate();
					cnt = db.InsertOrReplace((Animal)dog);
					validate();
					cnt = db.Update((Animal)dog);
					validate();
					db.Delete((Animal)dog);

					void validate()
					{
						Assert.AreEqual(1, cnt);

						var data = db.GetTable<Dog>().Where(_ => _.Id == 666).FirstOrDefault();
						Assert.IsNotNull(data);
						Assert.AreEqual(dog.Id            , data.Id);
						Assert.AreEqual(dog.AnimalType    , data.AnimalType);
						Assert.AreEqual(dog.AnimalType2   , data.AnimalType2);
						Assert.AreEqual(dog.Name          , data.Name);
						Assert.AreEqual(dog.DogName.First , data.DogName.First);
						Assert.AreEqual(dog.DogName.Second, data.DogName.Second);
						Assert.AreEqual(dog.EyeId         , data.EyeId);
					}
				}
				finally
				{
					CleanupData(db);
				}
			}
		}
	}
}
