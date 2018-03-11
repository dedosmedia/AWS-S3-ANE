package com.dedosmedia {
import flash.events.Event;

public class S3Event extends Event {

    public static const S3_EVENT:String = "s3Event";
    public static const CREDENTIALS_INVALID:String = "credentialsInvalid";
    public static const CLIENT_INITIALIZED:String = "clientInitialized";
    public static const OBJECT_UPLOADED:String = "objectUploaded";
    public static const NETWORK_ERROR:String = "networkError";
    public static const FILE_IO_ERROR:String = "fileIOError";
    public static const UNKNOWN_ERROR:String = "unknownError";
    public static const REGION_ERROR:String = "regionError";
    public static const BUCKET_ERROR:String = "bucketError";


    public var params:*;
    public function S3Event(type:String, params:* = null, bubbles:Boolean = false, cancelable:Boolean = false) {
        super(type, bubbles, cancelable);
        this.params = params;
    }
    public override function clone():Event {
        return new S3Event(type, this.params, bubbles, cancelable);
    }

    public override function toString():String {
        return formatToString("S3Event", "params", "type", "bubbles", "cancelable");
    }
}
}
