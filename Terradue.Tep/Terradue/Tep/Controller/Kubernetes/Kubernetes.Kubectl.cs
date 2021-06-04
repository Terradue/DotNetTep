using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Terradue.Tep.Controller.Kubernetes.Kubectl {


    /*********************/
    /**** Deployments ****/
    /*********************/

    [DataContract]
    public class KubectlLabels {
        [DataMember]
        public string app { get; set; }
    }

    [DataContract]
    public class KubectlMetadata {
        [DataMember]
        public string name { get; set; }
        [DataMember]
        public KubectlLabels labels { get; set; }
    }

    [DataContract]
    public class KubectlRequests {
        [DataMember]
        public string storage { get; set; }
    }

    [DataContract]
    public class KubectlResources {
        [DataMember]
        public KubectlRequests requests { get; set; }
    }

    [DataContract]
    public class KubectlSpec {
        [DataMember]
        public List<string> accessModes { get; set; }
        [DataMember]
        public KubectlResources resources { get; set; }
        [DataMember]
        public string storageClassName { get; set; }
        [DataMember]
        public bool automountServiceAccountToken { get; set; }
        [DataMember]
        public List<KubectlMetadata> imagePullSecrets { get; set; }
        [DataMember]
        public List<KubectlContainer> containers { get; set; }
        [DataMember]
        public List<KubectlVolume> volumes { get; set; }
        [DataMember]
        public KubectlNodeSelector nodeSelector { get; set; }
        [DataMember]
        public KubectlSelector selector { get; set; }
        [DataMember]
        public KubectlTemplate template { get; set; }
    }

    [DataContract]
    public class KubectlSelector {
        [DataMember]
        public KubectlLabels matchLabels { get; set; }
    }

    [DataContract]
    public class KubectlContainer {
        [DataMember]
        public string name { get; set; }
        [DataMember]
        public string image { get; set; }
        [DataMember]
        public List<KubectlPort> ports { get; set; }
        [DataMember]
        public List<KubectlVolumeMount> volumeMounts { get; set; }
    }

    [DataContract]
    public class KubectlVolumeMount {
        [DataMember]
        public string name { get; set; }
        [DataMember]
        public string mountPath { get; set; }
        [DataMember]
        public bool readOnly { get; set; }
    }

    [DataContract]
    public class KubectlPort {
        [DataMember]
        public int containerPort { get; set; }
    }

    [DataContract]
    public class KubectlVolume {
        [DataMember]
        public string name { get; set; }
        [DataMember]
        public KubectlNfs nfs { get; set; }
        [DataMember]
        public KubectlPersistentVolumeClaim persistentVolumeClaim { get; set; }
    }

    [DataContract]
    public class KubectlNfs {
        [DataMember]
        public string server { get; set; }
        [DataMember]
        public string path { get; set; }
        [DataMember]
        public bool readOnly { get; set; }
    }

    [DataContract]
    public class KubectlPersistentVolumeClaim {
        [DataMember]
        public string claimName { get; set; }
    }

    [DataContract]
    public class KubectlNodeSelector {
        [DataMember]
        public string application { get; set; }
    }

    [DataContract]
    public class KubectlTemplate {
        [DataMember]
        public KubectlMetadata metadata { get; set; }
        [DataMember]
        public KubectlSpec spec { get; set; }
    }

    [DataContract]
    public class KubectlRequest {
        [DataMember]
        public string apiVersion { get; set; }
        [DataMember]
        public string kind { get; set; }
        [DataMember]
        public KubectlMetadata metadata { get; set; }
        [DataMember]
        public KubectlSpec spec { get; set; }
    }




    /**************/
    /**** Pods ****/
    /**************/

    [DataContract]
    public class PodMetadata {
        [DataMember]
        public string selfLink { get; set; }
        [DataMember]
        public string resourceVersion { get; set; }
        [DataMember]
        public string name { get; set; }
        [DataMember]
        public string generateName { get; set; }
        [DataMember]
        public string @namespace { get; set; }
        [DataMember]
        public string uid { get; set; }
        [DataMember]
        public string creationTimestamp { get; set; }
        [DataMember]
        public PodLabels labels { get; set; }
        [DataMember]
        public PodAnnotations annotations { get; set; }
        [DataMember]
        public List<PodOwnerReference> ownerReferences { get; set; }
    }

    [DataContract]
    public class PodLabels {
        [DataMember]
        public string app { get; set; }
        [DataMember]
        public string environment { get; set; }
        [DataMember(Name = "pod-template-hash")]
        public string PodTemplateHash { get; set; }
        [DataMember(Name = "controller-revision-hash")]
        public string ControllerRevisionHash { get; set; }
        [DataMember(Name = "statefulset.kubernetes.io/pod-name")]
        public string StatefulsetKubernetesIoPodName { get; set; }
    }

    [DataContract]
    public class PodAnnotations {
        [DataMember(Name = "openshift.io/scc")]
        public string OpenshiftIoScc { get; set; }
    }

    [DataContract]
    public class PodOwnerReference {
        [DataMember]
        public string apiVersion { get; set; }
        [DataMember]
        public string kind { get; set; }
        [DataMember]
        public string name { get; set; }
        [DataMember]
        public string uid { get; set; }
        [DataMember]
        public bool controller { get; set; }
        [DataMember]
        public bool blockOwnerDeletion { get; set; }
    }

    [DataContract]
    public class PersistentVolumeClaim {
        [DataMember]
        public string claimName { get; set; }
    }

    [DataContract]
    public class Nfs {
        [DataMember]
        public string server { get; set; }
        [DataMember]
        public string path { get; set; }
        [DataMember]
        public bool readOnly { get; set; }
    }

    [DataContract]
    public class Volume {
        [DataMember]
        public string name { get; set; }
        [DataMember]
        public PersistentVolumeClaim persistentVolumeClaim { get; set; }
        [DataMember]
        public Nfs nfs { get; set; }
    }

    [DataContract]
    public class Port {
        [DataMember]
        public int containerPort { get; set; }
        [DataMember]
        public string protocol { get; set; }
        [DataMember]
        public string name { get; set; }
    }

    [DataContract]
    public class Env {
        [DataMember]
        public string name { get; set; }
        [DataMember]
        public string value { get; set; }
    }

    [DataContract]
    public class VolumeMount {
        [DataMember]
        public string name { get; set; }
        [DataMember]
        public string mountPath { get; set; }
        [DataMember]
        public bool? readOnly { get; set; }
    }

    [DataContract]
    public class Capabilities {
        [DataMember]
        public List<string> drop { get; set; }
    }

    [DataContract]
    public class SecurityContext {
        [DataMember]
        public Capabilities capabilities { get; set; }
        [DataMember]
        public SeLinuxOptions seLinuxOptions { get; set; }
    }

    [DataContract]
    public class Resources {
    }

    [DataContract]
    public class ConfigMapRef {
        [DataMember]
        public string name { get; set; }
    }

    [DataContract]
    public class EnvFrom {
        [DataMember]
        public ConfigMapRef configMapRef { get; set; }
    }

    [DataContract]
    public class Container {
        [DataMember]
        public string name { get; set; }
        [DataMember]
        public string image { get; set; }
        [DataMember]
        public List<Port> ports { get; set; }
        [DataMember]
        public List<Env> env { get; set; }
        [DataMember]
        public Resources resources { get; set; }
        [DataMember]
        public List<VolumeMount> volumeMounts { get; set; }
        [DataMember]
        public string terminationMessagePath { get; set; }
        [DataMember]
        public string terminationMessagePolicy { get; set; }
        [DataMember]
        public string imagePullPolicy { get; set; }
        [DataMember]
        public SecurityContext securityContext { get; set; }
        [DataMember]
        public List<EnvFrom> envFrom { get; set; }
    }

    [DataContract]
    public class NodeSelector {
        [DataMember(Name = "node-role.kubernetes.io/compute")]
        public string NodeRoleKubernetesIoCompute { get; set; }
        [DataMember]
        public string application { get; set; }
    }

    [DataContract]
    public class SeLinuxOptions {
        [DataMember]
        public string level { get; set; }
    }

    [DataContract]
    public class ImagePullSecret {
        [DataMember]
        public string name { get; set; }
    }

    [DataContract]
    public class PodSpec {
        [DataMember]
        public List<string> accessModes { get; set; }
        [DataMember]
        public KubectlResources resources { get; set; }
        [DataMember]
        public string storageClassName { get; set; }
        [DataMember]
        public KubectlSelector selector { get; set; }
        [DataMember]
        public int replicas { get; set; }
        [DataMember]
        public KubectlTemplate template { get; set; }
        [DataMember]
        public List<Volume> volumes { get; set; }
        [DataMember]
        public List<Container> containers { get; set; }
        [DataMember]
        public string restartPolicy { get; set; }
        [DataMember]
        public int terminationGracePeriodSeconds { get; set; }
        [DataMember]
        public string dnsPolicy { get; set; }
        [DataMember]
        public NodeSelector nodeSelector { get; set; }
        [DataMember]
        public string serviceAccountName { get; set; }
        [DataMember]
        public string serviceAccount { get; set; }
        [DataMember]
        public string nodeName { get; set; }
        [DataMember]
        public SecurityContext securityContext { get; set; }
        [DataMember]
        public List<ImagePullSecret> imagePullSecrets { get; set; }
        [DataMember]
        public string schedulerName { get; set; }
        [DataMember]
        public int priority { get; set; }
        [DataMember]
        public string hostname { get; set; }
        [DataMember]
        public string subdomain { get; set; }
    }

    [DataContract]
    public class Condition {
        [DataMember]
        public string type { get; set; }
        [DataMember]
        public string status { get; set; }
        [DataMember]
        public object lastProbeTime { get; set; }
        [DataMember]
        public string lastTransitionTime { get; set; }
    }

    [DataContract]
    public class Running {
        [DataMember]
        public string startedAt { get; set; }
    }

    [DataContract]
    public class State {
        [DataMember]
        public Running running { get; set; }
    }

    [DataContract]
    public class LastState {
    }

    [DataContract]
    public class ContainerStatus {
        [DataMember]
        public string name { get; set; }
        [DataMember]
        public State state { get; set; }
        [DataMember]
        public LastState lastState { get; set; }
        [DataMember]
        public bool ready { get; set; }
        [DataMember]
        public int restartCount { get; set; }
        [DataMember]
        public string image { get; set; }
        [DataMember]
        public string imageID { get; set; }
        [DataMember]
        public string containerID { get; set; }
    }

    [DataContract]
    public class PodStatus {
        [DataMember]
        public string phase { get; set; }
        [DataMember]
        public List<Condition> conditions { get; set; }
        [DataMember]
        public string hostIP { get; set; }
        [DataMember]
        public string podIP { get; set; }
        [DataMember]
        public string startTime { get; set; }
        [DataMember]
        public List<ContainerStatus> containerStatuses { get; set; }
        [DataMember]
        public string qosClass { get; set; }
    }

    [DataContract]
    public class KubectlPod {
        [DataMember]
        public PodMetadata metadata { get; set; }
        [DataMember]
        public PodSpec spec { get; set; }
        [DataMember]
        public PodStatus status { get; set; }
        [DataMember]
        public string apiVersion { get; set; }
        [DataMember]
        public string kind { get; set; }

        public string IP {
            get {
                if (status != null) return status.podIP;
                else return null;
            }
        }
        public string Identifier {
            get {
                if (metadata != null) return metadata.name;
                else return null;
            }
        }
    }

    [DataContract]
    public class KubectlPods {
        [DataMember]
        public string kind { get; set; }
        [DataMember]
        public string apiVersion { get; set; }
        [DataMember]
        public PodMetadata metadata { get; set; }
        [DataMember]
        public List<KubectlPod> items { get; set; }
    }


}
