/**
 * Created by Eoin Landy on 29/04/2017.
 */
package com.tuarua.fre {
import flash.system.Capabilities;

[RemoteClass(alias="com.tuarua.fre.ANEError")]
public class ANEError extends Error {
    private var _stackTrace:String;
    private var _source:String;
    private var _type:String;

    private var errorTypesCSharp:Array = [
        "FreSharp.Exceptions.Ok",
        "FreSharp.Exceptions.NoSuchNameException",
        "FreSharp.Exceptions.FreInvalidObjectException",
        "FreSharp.Exceptions.FreTypeMismatchException",
        "FreSharp.Exceptions.FreActionscriptErrorException",
        "FreSharp.Exceptions.FreInvalidArgumentException",
        "FreSharp.Exceptions.FreReadOnlyException",
        "FreSharp.Exceptions.FreWrongThreadException",
        "FreSharp.Exceptions.FreIllegalStateException",
        "FreSharp.Exceptions.FreInsufficientMemoryException"
    ];

    private var errorTypesSwift:Array = [
        "ok",
        "noSuchName",
        "invalidObject",
        "typeMismatch",
        "actionscriptError",
        "invalidArgument",
        "readOnly",
        "wrongThread",
        "illegalState",
        "insufficientMemory"
    ];

    public function ANEError(message:String, errorID:int, type:String, source:String, stackTrace:String) {
        _stackTrace = stackTrace;
        _source = source;
        _type = type;
        super(message, getErrorID(_type));
    }

    override public function get errorID():int {
        return super.errorID;
    }

    override public function getStackTrace():String {
        return _stackTrace;
    }

    private function getErrorID(type:String):int {
        var val:int;
        if (Capabilities.os.toLowerCase().indexOf("win") == 0) {
            val = errorTypesCSharp.indexOf(type);
        } else {
            val = errorTypesSwift.indexOf(type);
        }
        if (val == -1) val = 10;
        return val;
    }

    public function get type():String {
        return _type;
    }

    public function get source():String {
        return _source;
    }
}
}