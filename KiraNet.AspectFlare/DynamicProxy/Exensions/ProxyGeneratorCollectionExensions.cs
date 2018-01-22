//using System;

//namespace KiraNet.AspectFlare.DynamicProxy.Exensions
//{
//    public static class ProxyGeneratorCollectionExensions
//    {
//        public static IProxyGeneratorCollection AddProxy(this IProxyGeneratorCollection collection, Type classType)
//        {
//            if (classType == null)
//            {
//                throw new ArgumentNullException(nameof(classType));
//            }

//            if (classType.IsValueType)
//            {
//                throw new ArgumentException(nameof(classType));
//            }

//            return collection.AddProxy(new ProxyDescriptor
//            {
//                InterfaceType = classType,
//                ClassType = classType
//            });
//        }

//        public static IProxyGeneratorCollection AddProxy(this IProxyGeneratorCollection collection, Type interfaceType, Type classType)
//        {
//            if (interfaceType == null)
//            {
//                throw new ArgumentNullException(nameof(interfaceType));
//            }

//            if (classType == null)
//            {
//                throw new ArgumentNullException(nameof(classType));
//            }

//            if (interfaceType.IsValueType)
//            {
//                throw new ArgumentException(nameof(interfaceType));
//            }

//            if (classType.IsValueType)
//            {
//                throw new ArgumentException(nameof(classType));
//            }

//            return collection.AddProxy(new ProxyDescriptor
//            {
//                InterfaceType = interfaceType,
//                ClassType = classType
//            });
//        }

//        public static IProxyGeneratorCollection AddProxy<TClass>(this IProxyGeneratorCollection collection)
//            where TClass : class

//        {
//            var type = typeof(TClass);
//            return collection.AddProxy(new ProxyDescriptor
//            {
//                InterfaceType = type,
//                ClassType = type
//            });
//        }


//        public static IProxyGeneratorCollection AddProxy<TInterface, TClass>(
//            this IProxyGeneratorCollection collection)
//            where TInterface : class
//            where TClass : class, TInterface
//        {
//            return collection.AddProxy(new ProxyDescriptor
//            {
//                InterfaceType = typeof(TInterface),
//                ClassType = typeof(TClass)
//            });
//        }
//    }
//}
