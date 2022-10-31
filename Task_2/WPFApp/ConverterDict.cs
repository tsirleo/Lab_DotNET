using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace WPFApp
{
    public class ConverterDict: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                string output = "";
                var dict = new Dictionary<string, double> ((Dictionary<string, double>)value);
                var orderedDict = dict.OrderByDescending(pair => pair.Value).ToDictionary(pair => pair.Key, pair => pair.Value);
                int indexer = 1;

                foreach (var emotion in orderedDict)
                {
                    output += $"{indexer}) {emotion.Key} = {string.Format("{0:f3}", emotion.Value*100)}%\n";
                    indexer += 1;
                }
                return output;
            }
            catch (Exception)
            {
                return "";
            }
        }
        public object ConvertBack(object value, Type targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
