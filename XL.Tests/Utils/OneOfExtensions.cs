﻿using OneOf;
using XL.API.Data.Models;
using XL.API.Models;

namespace XL.Tests.Utils;

public static class OneOfExtensions
{
    public static bool IsSuccess(this OneOf<Success<SheetCell>, Unprocessable> result) => result.IsT0;

    public static bool IsError(this OneOf<Success<SheetCell>, Unprocessable> result) => result.IsT1;
}