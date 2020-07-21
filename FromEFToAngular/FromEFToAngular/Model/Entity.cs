using System;
using System.Collections.Generic;
using System.Text;

namespace FromEFToAngular.Model
{
    class Entity
    {
        public string Name { get; set; }
        public List<EntityProperty> Properties { get; set; }
    }
}
