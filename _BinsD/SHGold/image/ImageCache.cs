using System;
using System.Collections.Generic;
using System.Text;

using System.Collections;
using System.Drawing.Imaging;
using System.Drawing;

using ylink.comm;

namespace ylink.image
{
    /// <summary>
    /// Õº∆¨ª∫¥Ê¿‡
    /// </summary>
    public class ImageCache
    {
        private static Hashtable m_htImages = new Hashtable();

        /// <summary>
        /// ªÒ»°Õº∆¨
        /// </summary>
        /// <param name="v_sImgFilePath"></param>
        /// <returns></returns>
        public static Image GetImage(string v_sImgFilePath)
        {
            try
            {
                Image image = (Image)m_htImages[v_sImgFilePath];
                if (image == null)
                {
                    image = Image.FromFile(v_sImgFilePath);
                    m_htImages.Add(v_sImgFilePath, image);
                }
                return image;
            }
            catch (Exception e)
            {
                string msg = "¥¥Ω®Õº∆¨¥ÌŒÛ£∫FileName=[" + v_sImgFilePath + "]";
                msg += CommUtil.GetExceptionMsg(e);
                CommUtil.ShowErrMsg(msg);
                return null;
            }
        }
    }
}
