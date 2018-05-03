using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using CL = OpenCL.Net;
using OpenCL.Net;

namespace HolaDownloader
{
    class OCLResizer
    {
        private CL.Context OCLContext;
        private CL.Program OCLProgram;
        //private CL.Kernel OCLKernelResizeImage;
        //private CL.Kernel OCLKernelGrayscaleImage;
        CommandQueue cmdQueue;

        List<CL.Device> devicesList = new List<CL.Device>();
        private void ContextNotify(string errInfo, byte[] data, IntPtr cb, IntPtr userData)
        {
            Console.WriteLine("OpenCL Notification: " + errInfo);
        }
        public void Init()
        {
            CL.ErrorCode error;
            CL.Platform[] platforms = Cl.GetPlatformIDs(out error);

            foreach (CL.Platform platform in platforms)
            {
                string platformName = Cl.GetPlatformInfo(platform, PlatformInfo.Name, out error).ToString();
                Console.WriteLine("Platform: " + platformName);

                foreach (CL.Device device in Cl.GetDeviceIDs(platform, CL.DeviceType.Gpu, out error))
                {
                    //Console.WriteLine("Device: " + device.ToString());
                    Console.WriteLine("Device: " + Cl.GetDeviceInfo(device, DeviceInfo.Name, out error));
                    Console.WriteLine("Image support: " + Cl.GetDeviceInfo(device, DeviceInfo.ImageSupport, out error).
                        CastTo<OpenCL.Net.Bool>());
                    devicesList.Add(device);
                }
            }

            if (devicesList.Count <= 0)
            {
                Console.WriteLine("No devices found.");
                return;
            };

            OCLContext = Cl.CreateContext(null, 1, devicesList.ToArray(), ContextNotify, IntPtr.Zero, out error);
            OCLProgram = Cl.CreateProgramWithSource(OCLContext, 1, new[] { programSource }, null, out error);
            error = Cl.BuildProgram(OCLProgram, 1, devicesList.ToArray(), string.Empty, null, IntPtr.Zero);

            if (Cl.GetProgramBuildInfo(OCLProgram, devicesList[0], 
                OpenCL.Net.ProgramBuildInfo.Status, out error).CastTo<OpenCL.Net.BuildStatus>()!= OpenCL.Net.BuildStatus.Success)
            {
                Console.WriteLine("Cl.GetProgramBuildInfo != Success");
                Console.WriteLine(Cl.GetProgramBuildInfo(OCLProgram, devicesList[0], OpenCL.Net.ProgramBuildInfo.Log, out error));
                return;
            }

            //OCLKernelResizeImage = Cl.CreateKernel(OCLProgram, "ResizeImage", out error);
            //OCLKernelGrayscaleImage = Cl.CreateKernel(OCLProgram, "GrayscaleImage", out error);
            cmdQueue = Cl.CreateCommandQueue(OCLContext, devicesList[0], (CommandQueueProperties)0, out error);
        }

        public void Grayscale(BitmapData bitmapData, int inputImgWidth, int inputImgHeight, out Bitmap DestImg)
        {
            int intPtrSize = 0;
            intPtrSize = Marshal.SizeOf(typeof(IntPtr));
            byte[] inputByteArray;
            CL.ImageFormat clImageFormat = new OpenCL.Net.ImageFormat(CL.ChannelOrder.RGBA, CL.ChannelType.Unsigned_Int8);

            int inputImgBytesSize;
            int inputImgStride;
            CL.ErrorCode error;
            CL.Kernel OCLKernelGrayscaleImage = Cl.CreateKernel(OCLProgram, "GrayscaleImage", out error);
            
            //inputImgWidth = SrcImg.Width;
            //inputImgHeight = SrcImg.Height;

            //System.Drawing.Bitmap bmpImage = new System.Drawing.Bitmap(SrcImg);
            //BitmapData bitmapData = SrcImg.LockBits(new Rectangle(0, 0, SrcImg.Width, SrcImg.Height),
            //                                          ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);//inputImage.PixelFormat);

            inputImgStride = bitmapData.Stride;
            inputImgBytesSize = bitmapData.Stride * bitmapData.Height;

            inputByteArray = new byte[inputImgBytesSize];
            Marshal.Copy(bitmapData.Scan0, inputByteArray, 0, inputImgBytesSize);

            var inputImage2DBuffer = Cl.CreateImage2D(OCLContext, MemFlags.CopyHostPtr | MemFlags.ReadOnly, clImageFormat,
                                                (IntPtr)bitmapData.Width, (IntPtr)bitmapData.Height,
                                                (IntPtr)0, inputByteArray, out error);

            byte[] outputByteArray = new byte[inputImgBytesSize];

            var outputImage2DBuffer = Cl.CreateImage2D(OCLContext, MemFlags.CopyHostPtr | MemFlags.WriteOnly, clImageFormat,
                                                          (IntPtr)inputImgWidth, (IntPtr)inputImgHeight, (IntPtr)0, outputByteArray, out error);

            error = Cl.SetKernelArg(OCLKernelGrayscaleImage, 0, (IntPtr)intPtrSize, inputImage2DBuffer);
            error |= Cl.SetKernelArg(OCLKernelGrayscaleImage, 1, (IntPtr)intPtrSize, outputImage2DBuffer);


            OpenCL.Net.Event clevent;

            IntPtr[] originPtr = new IntPtr[] { (IntPtr)0, (IntPtr)0, (IntPtr)0 };  //x, y, z
            IntPtr[] regionPtr = new IntPtr[] { (IntPtr)inputImgWidth, (IntPtr)inputImgHeight, (IntPtr)1 }; //x, y, z
            IntPtr[] workGroupSizePtr = new IntPtr[] { (IntPtr)inputImgWidth, (IntPtr)inputImgHeight, (IntPtr)1 };
            error = Cl.EnqueueWriteImage(cmdQueue, inputImage2DBuffer, OpenCL.Net.Bool.True, originPtr, regionPtr, (IntPtr)0, (IntPtr)0, inputByteArray, 0, null, out clevent);
            Cl.WaitForEvents(1, new CL.Event[] { clevent });
            error = Cl.EnqueueNDRangeKernel(cmdQueue, OCLKernelGrayscaleImage, 2, null, workGroupSizePtr, null, 0, null, out clevent);

            Cl.WaitForEvents(1, new CL.Event[] { clevent });
            //error = Cl.Finish(cmdQueue);

            error = Cl.EnqueueReadImage(cmdQueue, outputImage2DBuffer, OpenCL.Net.Bool.True, originPtr, regionPtr,
                                        (IntPtr)0, (IntPtr)0, outputByteArray, 0, null, out clevent);
            Cl.WaitForEvents(1, new CL.Event[] { clevent });
            //Cl.ReleaseKernel(OCLKernel);
            //Cl.ReleaseCommandQueue(cmdQueue);

            Cl.ReleaseMemObject(inputImage2DBuffer);
            Cl.ReleaseMemObject(outputImage2DBuffer);

            GCHandle pinnedOutputArray = GCHandle.Alloc(outputByteArray, GCHandleType.Pinned);
            IntPtr outputBmpPointer = pinnedOutputArray.AddrOfPinnedObject();

            DestImg = new Bitmap(inputImgWidth, inputImgHeight, inputImgStride, PixelFormat.Format32bppArgb, outputBmpPointer);

            
            pinnedOutputArray.Free();


        }

        public void Resize(BitmapData bitmapData, int inputImgWidth, int inputImgHeight, int outputImgWidth, int outputImgHeight, out Bitmap DestImg)
        {
            int intPtrSize = 0;
            intPtrSize = Marshal.SizeOf(typeof(IntPtr));
            byte[] inputByteArray;
            CL.ImageFormat clImageFormat = new OpenCL.Net.ImageFormat(CL.ChannelOrder.RGBA, CL.ChannelType.Unsigned_Int8);

            int inputImgBytesSize;
            int inputImgStride;
            CL.ErrorCode error;
            CL.Kernel OCLKernel = Cl.CreateKernel(OCLProgram, "ResizeImage", out error);

            inputImgStride = bitmapData.Stride;
            inputImgBytesSize = bitmapData.Stride * bitmapData.Height;

            inputByteArray = new byte[inputImgBytesSize];
            Marshal.Copy(bitmapData.Scan0, inputByteArray, 0, inputImgBytesSize);

            var inputImage2DBuffer = Cl.CreateImage2D(OCLContext, MemFlags.CopyHostPtr | MemFlags.ReadOnly, clImageFormat,
                                                (IntPtr)bitmapData.Width, (IntPtr)bitmapData.Height,
                                                (IntPtr)0, inputByteArray, out error);

            byte[] outputByteArray = new byte[inputImgBytesSize];

            var outputImage2DBuffer = Cl.CreateImage2D(OCLContext, MemFlags.CopyHostPtr | MemFlags.WriteOnly, clImageFormat,
                                                          (IntPtr)outputImgWidth, (IntPtr)outputImgWidth, (IntPtr)0, outputByteArray, out error);

            error = Cl.SetKernelArg(OCLKernel, 0, (IntPtr)intPtrSize, inputImage2DBuffer);
            error |= Cl.SetKernelArg(OCLKernel, 1, (IntPtr)intPtrSize, outputImage2DBuffer);


            CL.Event clevent;

            IntPtr[] originPtr = new IntPtr[] { (IntPtr)0, (IntPtr)0, (IntPtr)0 };  //x, y, z
            IntPtr[] regionPtr = new IntPtr[] { (IntPtr)inputImgWidth, (IntPtr)inputImgHeight, (IntPtr)1 }; //x, y, z
            IntPtr[] workGroupSizePtr = new IntPtr[] { (IntPtr)inputImgWidth, (IntPtr)inputImgHeight, (IntPtr)1 };
            error = Cl.EnqueueWriteImage(cmdQueue, inputImage2DBuffer, OpenCL.Net.Bool.True, originPtr, regionPtr, (IntPtr)0, (IntPtr)0, inputByteArray, 0, null, out clevent);
            Cl.WaitForEvents(1, new CL.Event[] { clevent });
            error = Cl.EnqueueNDRangeKernel(cmdQueue, OCLKernel, 2, null, workGroupSizePtr, null, 0, null, out clevent);

            Cl.WaitForEvents(1, new CL.Event[] { clevent });

            error = Cl.EnqueueReadImage(cmdQueue, outputImage2DBuffer, OpenCL.Net.Bool.True, originPtr, regionPtr,
                                        (IntPtr)0, (IntPtr)0, outputByteArray, 0, null, out clevent);
            Cl.WaitForEvents(1, new CL.Event[] { clevent });
            Cl.ReleaseKernel(OCLKernel);

            //Cl.ReleaseCommandQueue(cmdQueue);

            Cl.ReleaseMemObject(inputImage2DBuffer);
            Cl.ReleaseMemObject(outputImage2DBuffer);

            GCHandle pinnedOutputArray = GCHandle.Alloc(outputByteArray, GCHandleType.Pinned);
            IntPtr outputBmpPointer = pinnedOutputArray.AddrOfPinnedObject();

            DestImg = new Bitmap(inputImgWidth, inputImgHeight, inputImgStride, PixelFormat.Format32bppArgb, outputBmpPointer);

            
            pinnedOutputArray.Free();
        }


        string programSource = @"__kernel void GrayscaleImage(__read_only  image2d_t srcImg, 
                       __write_only image2d_t dstImg)
{
  const sampler_t smp = CLK_NORMALIZED_COORDS_FALSE | //Natural coordinates
    CLK_ADDRESS_CLAMP_TO_EDGE | //Clamp to zeros
    CLK_FILTER_LINEAR;

  int2 coord = (int2)(get_global_id(0), get_global_id(1)); 
  
  uint4 bgra = read_imageui(srcImg, smp, coord);	//The byte order is BGRA 
  
  float4 bgrafloat = convert_float4(bgra) / 255.0f;	//Convert to normalized [0..1] float
  
  //Convert RGB to luminance (make the image grayscale).
  float luminance =  sqrt(0.241f * bgrafloat.z * bgrafloat.z + 0.691f * bgrafloat.y * bgrafloat.y + 0.068f * bgrafloat.x * bgrafloat.x);
  bgra.x = bgra.y = bgra.z = (uint) (luminance * 255.0f);
  
  bgra.w = 255;
  
  write_imageui(dstImg, coord, bgra);
};


const sampler_t samplerIn =
    CLK_NORMALIZED_COORDS_TRUE |
    CLK_ADDRESS_CLAMP |
    CLK_FILTER_LINEAR;

const sampler_t samplerOut =
    CLK_NORMALIZED_COORDS_FALSE |
    CLK_ADDRESS_CLAMP |
    CLK_FILTER_NEAREST;

__kernel void ResizeImage(
    __read_only  image2d_t sourceImage,
    __write_only image2d_t targetImage)
{
    int w = get_image_width(targetImage);
    int h = get_image_height(targetImage);

    int2 posOut = {0, 0};

    float2 posIn = (float2) (0, 0);

    float4 pixel = read_imagef(sourceImage, samplerIn, posIn);
    write_imagef(targetImage, posOut, pixel);
}
";

    }
}
