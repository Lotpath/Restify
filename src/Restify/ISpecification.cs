using System.Collections.Generic;

namespace Restify
{
    public interface ISpecification
    {
        string ApiPath();
        string SqlQuery();
        IList<object> SqlParameters();
    }
}