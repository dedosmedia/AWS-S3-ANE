package com.dedosmedia {
import com.tuarua.fre.ANEError;
import flash.events.EventDispatcher;
import flash.events.StatusEvent;
import flash.external.ExtensionContext;

public class AWSS3ANE extends EventDispatcher {
    private static const NAME:String = "AWSS3ANE";
    private var ctx:ExtensionContext;
    private static const TRACE:String = "TRACE";

    public function AWSS3ANE() {
        trace("[" + NAME + "] Initalizing ANE...");
        try {
            ctx = ExtensionContext.createExtensionContext("com.dedosmedia." + NAME, null);
            ctx.addEventListener(StatusEvent.STATUS, gotEvent);
        } catch (e:Error) {
            trace(e.name);
            trace(e.message);
            trace(e.getStackTrace());
            trace(e.errorID);
            trace("[" + NAME + "] ANE Not loaded properly.  Future calls will fail.");
        }
    }

    private function gotEvent(event:StatusEvent):void {
        switch (event.level) {
            case TRACE:
                trace("[" + NAME + "]", event.code);
                break;
            case "MY_EVENT":
                    dispatchEvent(new CustomEvent("MY_EVENT", event.code));
                break;
        }
    }


    public function startS3Uploading(jsonFilePath:String):Boolean {
        var theRet:* = ctx.call("startS3Uploading", jsonFilePath);
        if (theRet is ANEError) {
            throw theRet as ANEError;
        }
        return theRet as Boolean;
    }

    public function init(accessKey:String, secretKey:String, region:String = null):void {
        var theRet:* = ctx.call("init",accessKey, secretKey, region);
        if (theRet is ANEError) {
            throw theRet as ANEError;
        }
    }


    public function dispose():void {
        if (!ctx) {
            trace("[" + NAME + "] Error. ANE Already in a disposed or failed state...");
            return;
        }
        trace("[" + NAME + "] Unloading ANE...");
        ctx.removeEventListener(StatusEvent.STATUS, gotEvent);
        ctx.dispose();
        ctx = null;
    }


}
}