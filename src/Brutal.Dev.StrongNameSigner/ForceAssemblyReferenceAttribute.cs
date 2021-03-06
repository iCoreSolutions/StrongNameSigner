﻿using Brutal.Dev.StrongNameSigner;
using System;
using System.Linq;

namespace Brutal.Dev.StrongNameSigner
{
  /// <summary>
  /// Attribute used to force an assembly reference to a specific assembly to be actually referenced and copied 
  /// locally.  
  /// See https://stackoverflow.com/questions/15816769/dependent-dll-is-not-getting-copied-to-the-build-output-folder-in-visual-studio
  /// </summary>
  [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
  internal sealed class ForceAssemblyReferenceAttribute : Attribute
  {
    public ForceAssemblyReferenceAttribute(Type forcedType)
    {
      Action<Type> noop = _ => { };
      noop(forcedType);
    }
  }
}
