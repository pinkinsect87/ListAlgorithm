
export class ReturnTree {
    isSuccess: boolean;
    errorMessage: string;
    clientReportTree: ClientReportTreeNode[];
}

export class ClientReportTreeNode {
    text: string;
    items: ClientReportTreeNode[];
    expanded: boolean;
    hasChildren: boolean;
    imageUrl: string;
    nodeType: string;
    nodeFileId: number;
    nodeFileUri: string;
    fullFileName: string;
    publishingGroupId: string;
    pubGroupReportid: string;
    affiliateId: string;
}

export class ClientReportDownload {
    isSuccess: boolean;
    message: string;
    url: string;
}
