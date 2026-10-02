 
using Manager;
using UnityEngine;

namespace Riptide
{
    /// <summary>Contains Unity-specific extension methods for the <see cref="Message"/> class.</summary>
    public static class MessageExtensions
    {
        #region Cosmetics
        /// <inheritdoc cref="Add(Message, Vector2)"/>
        /// <remarks>Relying on the correct Add overload being chosen based on the parameter type can increase the odds of accidental type mismatches when retrieving data from a message. This method calls <see cref="Add(Message, Vector2)"/> and simply provides an alternative type-explicit way to add a <see cref="Vector2"/> to the message.</remarks>
        public static Message AddCosmeticIndex(this Message message, InventoryManager.CosmeticIndex value) => Add(message, value);

        /// <summary>Adds a <see cref="Vector2"/> to the message.</summary>
        /// <param name="value">The <see cref="Vector2"/> to add.</param>
        /// <returns>The message that the <see cref="Vector2"/> was added to.</returns>
        public static Message Add(this Message message, InventoryManager.CosmeticIndex value)
        {
            message.Add(value.weaponIndex);
            
            message.AddInt(value.hatIndex);
            if (value.hatIndex != -1)
            {
                message.AddColor(value.hatColor);
                message.AddFloat(value.hatShiny);
                message.AddInt(value.hatParticle);
            }
        
            message.AddInt(value.faceIndex);
            if (value.faceIndex != -1)
            {
                message.AddColor(value.faceColor);
                message.AddFloat(value.faceShiny);
                message.AddInt(value.faceParticle);
            }
        
            message.AddInt(value.shoesIndex);
            if (value.shoesIndex != -1)
            {
                message.AddColor(value.shoesColor);
                message.AddFloat(value.shoesShiny);
                message.AddInt(value.shoesParticle);
            }
        
            message.AddInt(value.hairIndex);
            if (value.hairIndex != -1)
            {
                message.AddColor(value.hairColor);
                message.AddFloat(value.hairShiny);
                message.AddInt(value.hairParticle);
            }
            
            message.AddInt(value.pantIndex);
            if (value.pantIndex != -1)
            {
                message.AddColor(value.pantColor);
                message.AddFloat(value.pantShiny);
                message.AddInt(value.pantParticle);
            }
            
            message.AddInt(value.clothesIndex);
            if (value.clothesIndex != -1)
            {
                message.AddColor(value.clothesColor);
                message.AddFloat(value.clothesShiny);
                message.AddInt(value.clothesParticle);
            }

            return message;
        }

        public static InventoryManager.CosmeticIndex GetCosmeticIndex(this Message message)
        {
            InventoryManager.CosmeticIndex cosmeticIndex = new InventoryManager.CosmeticIndex();

            cosmeticIndex.weaponIndex = message.GetUShorts();

            cosmeticIndex.hatIndex = message.GetInt();

            if (cosmeticIndex.hatIndex != -1)
            {
                cosmeticIndex.hatColor = message.GetColor();
                cosmeticIndex.hatShiny = message.GetFloat();
                cosmeticIndex.hatParticle = message.GetInt();
            }
            
            cosmeticIndex.faceIndex = message.GetInt();

            if (cosmeticIndex.faceIndex != -1)
            {
                cosmeticIndex.faceColor = message.GetColor();
                cosmeticIndex.faceShiny = message.GetFloat();
                cosmeticIndex.faceParticle = message.GetInt();
            }
            
            cosmeticIndex.shoesIndex = message.GetInt();

            if (cosmeticIndex.shoesIndex != -1)
            {
                cosmeticIndex.shoesColor = message.GetColor();
                cosmeticIndex.shoesShiny = message.GetFloat();
                cosmeticIndex.shoesParticle = message.GetInt();
            }
            
            cosmeticIndex.hairIndex = message.GetInt();

            if (cosmeticIndex.hairIndex != -1)
            {
                cosmeticIndex.hairColor = message.GetColor();
                cosmeticIndex.hairShiny = message.GetFloat();
                cosmeticIndex.hairParticle = message.GetInt();
            }
            
            cosmeticIndex.pantIndex = message.GetInt();

            if (cosmeticIndex.pantIndex != -1)
            {
                cosmeticIndex.pantColor = message.GetColor();
                cosmeticIndex.pantShiny = message.GetFloat();
                cosmeticIndex.pantParticle = message.GetInt();
            }
            
            cosmeticIndex.clothesIndex = message.GetInt();

            if (cosmeticIndex.clothesIndex != -1)
            {
                cosmeticIndex.clothesColor = message.GetColor();
                cosmeticIndex.clothesShiny = message.GetFloat();
                cosmeticIndex.clothesParticle = message.GetInt();
            }
            

            return cosmeticIndex;
        }
        
        #endregion
        #region Color
        /// <inheritdoc cref="Add(Message, Vector2)"/>
        /// <remarks>Relying on the correct Add overload being chosen based on the parameter type can increase the odds of accidental type mismatches when retrieving data from a message. This method calls <see cref="Add(Message, Vector2)"/> and simply provides an alternative type-explicit way to add a <see cref="Vector2"/> to the message.</remarks>
        public static Message AddColor(this Message message, Color value) => Add(message, value);

        /// <summary>Adds a <see cref="Vector2"/> to the message.</summary>
        /// <param name="value">The <see cref="Vector2"/> to add.</param>
        /// <returns>The message that the <see cref="Vector2"/> was added to.</returns>
        public static Message Add(this Message message, Color value)
        {
            message.AddFloat(value.r);
            message.AddFloat(value.g);
            message.AddFloat(value.b);
            message.AddFloat(value.a);
            return message;
        }

        /// <summary>Retrieves a <see cref="Vector2"/> from the message.</summary>
        /// <returns>The <see cref="Vector2"/> that was retrieved.</returns>
        public static Color GetColor(this Message message)
        {
            return new Color(message.GetFloat(), message.GetFloat(),message.GetFloat(),message.GetFloat());
        }
        #endregion
        #region Vector2
        /// <inheritdoc cref="AddVector2(Message, Vector2)"/>
        /// <remarks>This method is simply an alternative way of calling <see cref="AddVector2(Message, Vector2)"/>.</remarks>
        public static Message Add(this Message message, Vector2 value) => AddVector2(message, value);

        /// <summary>Adds a <see cref="Vector2"/> to the message.</summary>
        /// <param name="value">The <see cref="Vector2"/> to add.</param>
        /// <returns>The message that the <see cref="Vector2"/> was added to.</returns>
        public static Message AddVector2(this Message message, Vector2 value)
        {
            return message.AddFloat(value.x).AddFloat(value.y);
        }

        /// <summary>Retrieves a <see cref="Vector2"/> from the message.</summary>
        /// <returns>The <see cref="Vector2"/> that was retrieved.</returns>
        public static Vector2 GetVector2(this Message message)
        {
            return new Vector2(message.GetFloat(), message.GetFloat());
        }
        #endregion

        #region Vector3
        /// <inheritdoc cref="AddVector3(Message, Vector3)"/>
        /// <remarks>This method is simply an alternative way of calling <see cref="AddVector3(Message, Vector3)"/>.</remarks>
        public static Message Add(this Message message, Vector3 value) => AddVector3(message, value);

        /// <summary>Adds a <see cref="Vector3"/> to the message.</summary>
        /// <param name="value">The <see cref="Vector3"/> to add.</param>
        /// <returns>The message that the <see cref="Vector3"/> was added to.</returns>
        public static Message AddVector3(this Message message, Vector3 value)
        {
            return message.AddFloat(value.x).AddFloat(value.y).AddFloat(value.z);
        }

        /// <summary>Retrieves a <see cref="Vector3"/> from the message.</summary>
        /// <returns>The <see cref="Vector3"/> that was retrieved.</returns>
        public static Vector3 GetVector3(this Message message)
        {
            return new Vector3(message.GetFloat(), message.GetFloat(), message.GetFloat());
        }
        #endregion

        #region Quaternion
        /// <inheritdoc cref="AddQuaternion(Message, Quaternion)"/>
        /// <remarks>This method is simply an alternative way of calling <see cref="AddQuaternion(Message, Quaternion)"/>.</remarks>
        public static Message Add(this Message message, Quaternion value) => AddQuaternion(message, value);

        /// <summary>Adds a <see cref="Quaternion"/> to the message.</summary>
        /// <param name="value">The <see cref="Quaternion"/> to add.</param>
        /// <returns>The message that the <see cref="Quaternion"/> was added to.</returns>
        public static Message AddQuaternion(this Message message, Quaternion value)
        {
            return message.AddFloat(value.x).AddFloat(value.y).AddFloat(value.z).AddFloat(value.w);
        }

        /// <summary>Retrieves a <see cref="Quaternion"/> from the message.</summary>
        /// <returns>The <see cref="Quaternion"/> that was retrieved.</returns>
        public static Quaternion GetQuaternion(this Message message)
        {
            return new Quaternion(message.GetFloat(), message.GetFloat(), message.GetFloat(), message.GetFloat());
        }
        #endregion
    }
}
