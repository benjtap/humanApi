using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaieApi.Utils
{
    public class ValidationTelephone
    {
        // Valider un numéro israélien
        public static bool EstNumeroIsraelienValide(string telephone)
        {
            // Format : +972 suivi de 9 chiffres (mobile : 50, 52, 53, 54, 55, 58)
            var regex = new System.Text.RegularExpressions.Regex(@"^\+972(50|52|53|54|55|58)\d{7}$");
            return regex.IsMatch(telephone);
        }

        // Normaliser un numéro israélien
        public static string NormaliserNumeroIsraelien(string telephone)
        {
            // Enlever espaces et tirets
            telephone = telephone.Replace(" ", "").Replace("-", "");

            // Si commence par 0, remplacer par +972
            if (telephone.StartsWith("0"))
            {
                telephone = "+972" + telephone.Substring(1);
            }

            // Si pas de +, ajouter +972
            if (!telephone.StartsWith("+"))
            {
                telephone = "+972" + telephone;
            }

            return telephone;
        }
    }
}
