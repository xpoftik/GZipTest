using System;

public static class ThrowIf
{
    public static class Argument
    {

        /// <summary>
        /// Throws an exception if value is null.
        /// 
        /// </summary>
        /// <param name="value">Argument value.</param>
        /// <param name="argName">Argument name.</param>
        /// <param name="message">Custom message.</param>
        public static void IsNull(object value, string argName, string message = "Argument value must be non null.")
        {
            if (value is null) {
                throw new ArgumentNullException(argName, message);
            }
        }



        /// <summary>
        /// Throws an exception if argument string value is empty or whitespace.
        /// 
        /// </summary>
        /// <param name="value">Argument value.</param>
        /// <param name="argName">Argument name.</param>
        /// <param name="message">Custom message.</param>
        public static void StringIsEmpty(string value, string argName, string message = "Argument must be non empty or whitespace string.")
        {
            if (String.IsNullOrWhiteSpace(value)) {
                throw new ArgumentException(message, argName);
            }
        }

        /// <summary>
        /// Throw an exception if value equals Guid.Empty [00000000-0000-0000-0000-000000000000]
        /// </summary>
        /// <param name="value"></param>
        /// <param name="argName"></param>
        /// <param name="message"></param>
        public static void GuidIsEmpty(Guid value, string argName, string message = "Guid must be non empty.")
        {
            if (value == Guid.Empty) {
                throw new ArgumentException(message, argName);
            }
        }

        ///// <summary>
        ///// Throw NotFound exception if target is null.
        ///// </summary>
        ///// <param name="obj"></param>
        ///// <param name="targetName"></param>
        ///// <param name="message"></param>
        //public static void NotFound(object obj, string targetName, string message = "")
        //{
        //    if (obj is null) {
        //        message = $"TargetName:{targetName}; Message: '{message}'";
        //        throw new NotFoundException(message);
        //    }
        //}

        /// <summary>
        /// Throws an exception if value is less than zero.
        /// 
        /// </summary>
        /// <param name="value">Argument value.</param>
        /// <param name="argName">Argument name.</param>
        /// <param name="message">Custom message.</param>
        public static void LessThanZero(int value, string argName, string message = "Argument must be greater or equals zero.")
        {
            if (value < 0) {
                throw new ArgumentException(message, argName);
            }
        }

        /// <summary>
        /// Throws an exception if value is less or equals zero.
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="argName"></param>
        /// <param name="message"></param>
        public static void LessOrEqualZero(int value, string argName, string message = "Argument must be greater than zero.")
        {
            if (value <= 0) {
                throw new ArgumentException(message, argName);
            }
        }

        /// <summary>
        /// Throws an exception if value is more than maxValue.
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="maxValue"></param>
        /// <param name="argName"></param>
        /// <param name="message"></param>
        public static void MoreThan(int value, int maxValue, string argName, string message = "Argument must be less than {0}.")
        {
            message = String.Format(message, maxValue);
            if (value > maxValue) {
                throw new ArgumentException(message, argName);
            }
        }

        /// <summary>
        /// Throws an exception if value is more than or equal maxValue .
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="maxValue"></param>
        /// <param name="argName"></param>
        /// <param name="message"></param>
        public static void MoreThanOrEqual(int value, int maxValue, string argName, string message = "Argument must be less than {0}.")
        {
            message = String.Format(message, maxValue);
            if (value >= maxValue) {
                throw new ArgumentException(message, argName);
            }
        }

        /// <summary>
        /// Throws an exception if value is less than zero.
        /// 
        /// </summary>
        /// <param name="value">Argument value.</param>
        /// <param name="argName">Argument name.</param>
        /// <param name="message">Custom message.</param>
        public static void LessThanZero(long value, string argName, string message = "Argument must be greater or equals zero.")
        {
            if (value < 0) {
                throw new ArgumentException(message, argName);
            }
        }

        /// <summary>
        /// Throws an exception if value is less or equals zero.
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="argName"></param>
        /// <param name="message"></param>
        public static void LessOrEqualZero(long value, string argName, string message = "Argument must be greater than zero.")
        {
            if (value <= 0) {
                throw new ArgumentException(message, argName);
            }
        }

        /// <summary>
        /// Throws an exception if value is more than maxValue.
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="maxValue"></param>
        /// <param name="argName"></param>
        /// <param name="message"></param>
        public static void MoreThan(long value, long maxValue, string argName, string message = "Argument must be less than {0}.")
        {
            message = String.Format(message, maxValue);
            if (value > maxValue) {
                throw new ArgumentException(message, argName);
            }
        }

        /// <summary>
        /// Throws an exception if value is more than or equal maxValue .
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="maxValue"></param>
        /// <param name="argName"></param>
        /// <param name="message"></param>
        public static void MoreThanOrEqual(long value, long maxValue, string argName, string message = "Argument must be less than {0}.")
        {
            message = String.Format(message, maxValue);
            if (value >= maxValue) {
                throw new ArgumentException(message, argName);
            }
        }
    }
}