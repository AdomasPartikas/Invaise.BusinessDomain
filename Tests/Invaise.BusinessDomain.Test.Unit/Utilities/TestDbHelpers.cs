using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Invaise.BusinessDomain.Test.Unit.Utilities;

public static class TestDbHelpers
{
    public static Mock<DbSet<T>> MockDbSet<T>(List<T> data) where T : class
    {
        var queryable = data.AsQueryable();
        var mockSet = new Mock<DbSet<T>>();
        
        mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());
        
        mockSet.Setup(m => m.Add(It.IsAny<T>())).Callback<T>(data.Add);
        mockSet.Setup(m => m.AddRange(It.IsAny<IEnumerable<T>>())).Callback<IEnumerable<T>>(entities => data.AddRange(entities));
        mockSet.Setup(m => m.Remove(It.IsAny<T>())).Callback<T>(t => data.Remove(t));
        
        mockSet.Setup(m => m.Find(It.IsAny<object[]>()))
            .Returns<object[]>(ids => data.FirstOrDefault(d => EF.Property<string>(d, "Id") == (string)ids[0]));
        
        mockSet.Setup(m => m.FindAsync(It.IsAny<object[]>()))
            .ReturnsAsync((object[] ids) => data.FirstOrDefault(d => EF.Property<string>(d, "Id") == (string)ids[0]));
        
        return mockSet;
    }
} 